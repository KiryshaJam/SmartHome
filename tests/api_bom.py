"""Disposable PostgreSQL/API only. Includes the PR1–3 search regression."""
from api_regression import *
import copy
from concurrent.futures import ThreadPoolExecutor

material_class=node('-materials')
metal=node('-metal',material_class,True)
assemblies=node('-assemblies',None,True)
units=request('GET','/measure-units')
unit=request('POST','/measure-units',dict(name=prefix+'piece',shortName=prefix+'pcs'))['id']
metre=request('POST','/measure-units',dict(name=prefix+'metre',shortName=prefix+'m'))['id']

def product(label, cls=assemblies, u=unit):
    p=request('POST','/products',dict(name=prefix+label,shortName=prefix+label,classNodeId=cls))['id']
    save(p,[],u=u)
    return p

def spec(p): return request('GET',f'/bom/products/{p}')
def save(p,positions,rules=None,calculations=None,u=unit,expected=None,status=(200,)):
    latest=spec(p)['revision']
    return request('PUT',f'/bom/products/{p}',dict(expectedRevisionId=latest['id'] if expected is None and latest else expected,unitId=u,positions=positions,rules=rules or [],calculations=calculations or {},reason='API regression'),expected=status)
def pos(key,p,amount=1,denom=1,**kw):
    r=spec(p)['revision']
    return dict(id=key,componentId=p,revisionId=r['id'],unitId=r['unitId'],amount=amount,forQuantity=denom,sortOrder=0,**kw)
def variant(p,values=None,rev=None,status=(200,)):
    return request('POST','/bom/variants',dict(revisionId=rev or spec(p)['revision']['id'],name=prefix+'variant',values=values or {}),expected=status)
def totals(v,q=1,c=None): return request('GET',f'/bom/variants/{v}/totals?quantity={q}'+('' if c is None else f'&classId={c}'))
def amounts(v,q=1,c=None): return {x['product_id']:x['required_quantity'] for x in totals(v,q,c)}

cable=product('-cable',metal,metre)
bolt=product('-bolt',metal)
radio=product('-radio',metal)
board=product('-board',metal)
# Configurable child prototype: use the existing catalog parameter definitions.
n=definition('-sensors',1)
w=definition('-wireless',3)
computed=definition('-computed',2)
for d,kind in [(n,1),(w,3),(computed,2)]:
    request('POST',f'/classnodes/{assemblies}/parameters',dict(parameterDefinitionId=d,sortOrder=d,isRequired=d!=computed,minNumberValue=1 if kind==1 else None,maxNumberValue=10 if kind==1 else None))
nk,wk,ck=f'p{n}',f'p{w}',f'p{computed}'
child=product('-control')
child_positions=[pos('cable',cable,formula=f'{nk} * 2',condition=f'{wk} == "wired"'),pos('radio',radio,condition=f'{wk} == "radio"'),pos('bolts',bolt,4,2),pos('board',board)]
rules=[dict(expression=f'{wk} == "wired" OR {wk} == "radio"',message='Unknown mode'),dict(expression=f'NOT ({wk} == "radio" AND {nk} > 4)',message='Radio supports at most 4'),dict(expression=f'NOT (has_{cable} AND has_{radio})',message='Incompatible components')]
save(child,child_positions,rules,{ck:f'{nk} * 3'})
values1={nk:2,wk:'wired'};values2={nk:3,wk:'radio'}
v1=variant(child,values1);v2=variant(child,values2)
# Relational persistence retains the existing string-valued JSON transport.
revision_payload=spec(child)['revision']
assert isinstance(revision_payload['document'],str)
assert isinstance(json.loads(revision_payload['document'])['positions'],list)
assert 'documentId' not in revision_payload and 'creationTransaction' not in revision_payload
for saved_variant in (v1,v2,request('GET',f'/bom/variants/{v1["id"]}')):
    assert isinstance(saved_variant['snapshot'],str)
    assert 'source' not in json.loads(saved_variant['snapshot'])
    assert 'creationTransaction' not in saved_variant
assert amounts(v1['id'],3,material_class)=={cable:12,bolt:6,board:3}
assert amounts(v2['id'],2,material_class)=={radio:2,bolt:4,board:2}
for bad in [{},{nk:0,wk:'wired'},{nk:11,wk:'wired'},{nk:'two',wk:'wired'},{nk:2.5,wk:'wired'},{nk:1e100,wk:'wired'},{nk:5,wk:'radio'},{nk:2,wk:'invalid'},{nk:2,wk:'wired','p999999':1}]: variant(child,bad,status=(400,))
# nested configuration with parameter propagation + repeated material
kit=product('-kit')
root_positions=[pos('control',child,bindings={nk:nk,wk:wk}),pos('more-bolts',bolt,3),pos('second-control',child,childVariantId=v2['id'])]
save(kit,root_positions)
kv=variant(kit,values1)
assert amounts(kv['id'],2,material_class)=={cable:8,bolt:14,board:4,radio:2}
tree=request('GET',f'/bom/variants/{kv["id"]}/tree?quantity=2')
assert len([x for x in tree if x['product_id']==bolt])==3
# modification inherits pinned base, then replaces/excludes/adds by stable position id
mod=request('POST','/bom/modifications',dict(baseRevisionId=spec(child)['revision']['id'],name=prefix+'mod',shortName=prefix+'mod',reason='Modification'))['productId']
assert len(spec(mod)['effective']['positions'])==4
save(mod,[pos('board',bolt,3),dict(id='bolts',excluded=True),pos('extra',bolt,2)],rules,{ck:f'{nk} * 4'})
mv=variant(mod,values1)
assert amounts(mv['id'],1,material_class)=={cable:4,bolt:5}
assert amounts(v1['id'],1,material_class)=={cable:4,bolt:2,board:1}
# Modification as a configured nested component.
save(kit,[pos('mod',mod,bindings={nk:nk,wk:wk})])
assert amounts(variant(kit,values2)['id'],1,material_class)=={radio:1,bolt:5}
# Orders keep current lines; configured specifications remain immutable.
order=request('POST','/bom/orders',dict(number=prefix+'order',date='2026-11-02',expectedUpdateToken=None,lines=[dict(variantId=v1['id'],quantity=3),dict(variantId=v2['id'],quantity=2)]))
oid=order['order']['id'];token=order['order']['updateToken']
assert request('GET',f'/bom/orders/{oid}')==order
assert len(order['lines'])==2
request('GET','/bom/orders/2147483647',expected=(404,))
old_totals=request('GET',f'/bom/orders/{oid}/totals?classId={material_class}')
assert {x['product_id']:x['required_quantity'] for x in old_totals}=={cable:12,bolt:10,board:5,radio:2}
old_revision=spec(child)['revision']['id']
changed=copy.deepcopy(child_positions);changed[0]['formula']=f'{nk} * 5'
save(child,changed,rules)
assert amounts(variant(child,values1)['id'],1,material_class)[cable]==10
assert amounts(v1['id'],1,material_class)[cable]==4
assert amounts(variant(mod,values1)['id'],1,material_class)[cable]==4
assert old_totals==request('GET',f'/bom/orders/{oid}/totals?classId={material_class}')
historical_spec=request('GET',f'/bom/products/{child}?revisionId={old_revision}')
assert historical_spec['revision']['id']==old_revision
# Equal sortOrder does not identify a position; use its stable key.
historical_cable=[p for p in historical_spec['effective']['positions'] if p['id']=='cable']
assert len(historical_cable)==1, historical_spec
assert historical_cable[0]['componentId']==cable, historical_cable
assert historical_cable[0]['formula']==f'{nk} * 2', historical_cable
newv=variant(child,values1)
request('PUT',f'/bom/orders/{oid}',dict(number=prefix+'order',date='2026-11-02',expectedUpdateToken=token,lines=[dict(variantId=newv['id'],quantity=2)]))
assert request('GET',f'/bom/orders/{oid}')['order']['updateToken']==token+1
request('PUT',f'/bom/orders/{oid}',dict(number=prefix+'order',date='2026-11-02',expectedUpdateToken=token,lines=[dict(variantId=v1['id'],quantity=1)]),expected=(409,))
request('POST','/bom/orders',dict(number=prefix+'bad',date='2026-11-02',lines=[dict(variantId=v1['id'],quantity=-1)]),expected=(400,))
assert all(o['number']!=prefix+'bad' for o in request('GET','/bom/orders'))
current_order=request('GET',f'/bom/orders/{oid}')['order']['updateToken']
duplicate_lines=[dict(variantId=v1['id'],quantity=1.25),dict(variantId=v1['id'],quantity=2.5)]
body=dict(number=prefix+'order',date='2026-11-02',expectedUpdateToken=current_order,lines=duplicate_lines)
with ThreadPoolExecutor(2) as pool:
    results=list(pool.map(lambda _:request('PUT',f'/bom/orders/{oid}',body,expected=(200,409)),range(2)))
assert sum(result is not None and 'order' in result for result in results)==1
saved=request('GET',f'/bom/orders/{oid}')
assert saved['order']['updateToken']==current_order+1 and saved['lines']==duplicate_lines
assert request('GET',f'/bom/orders/{oid}/totals')==totals(v1['id'],3.75)
# Static validation of runtime-edited expressions, unknowns, types and cycles.
for expr in ['p999999 + 2',f'{wk} * 2','1 +','System.IO.File.Delete("x")',r'"\q"']:
    bad=copy.deepcopy(changed);bad[0]['formula']=expr
    before=spec(child)['revision']['id'];save(child,bad,rules,status=(400,));assert spec(child)['revision']['id']==before
save(child,changed,rules,{ck:ck+' + 1'},status=(400,))
for expr in ['1 / 0','0','-1','1000000000 * 1000000000']:
    bad=copy.deepcopy(changed);bad[0]['formula']=expr
    save(child,bad,rules);variant(child,values1,status=(400,))
save(child,changed,rules)
# positive norms, units, direct/cross cycles, immutable base, stale optimistic writes
save(child,[pos('bad',bolt,0)],status=(400,))
save(child,[pos('bad/path',bolt)],status=(400,))
save(child,[pos('tiny',bolt,1,0)],status=(400,))
bad=pos('bad',bolt);bad['unitId']=metre;save(child,[bad],status=(400,))
save(child,[pos('self',child)],status=(400,))
save(kit,root_positions)
save(child,[pos('cycle',kit)],status=(400,))
request('DELETE',f'/products/{bolt}',expected=(409,))
# Concurrent update: one winner, one 409, no detached history.
current=spec(child)['revision']['id']
body=dict(expectedRevisionId=current,unitId=unit,positions=changed,rules=rules,reason='Concurrent')
with ThreadPoolExecutor(2) as pool:
    list(pool.map(lambda _: request('PUT',f'/bom/products/{child}',body,expected=(200,409)),range(2)))
assert len([x for x in request('GET',f'/bom/products/{child}/history') if x['previousId']==current])==1
# opposing writes are serialized; pinned old empty revisions cannot bypass current cycles
x,y=product('-cycle-x'),product('-cycle-y')
px,py=pos('x',x),pos('y',y)
with ThreadPoolExecutor(2) as pool:
    list(pool.map(lambda pair:save(pair[0],[pair[1]],status=(200,400)),[(x,py),(y,px)]))
assert sum(len(spec(p)['effective']['positions']) for p in [x,y])==1
# Component compatibility and conflicting restrictions reject the whole variant.
bad=copy.deepcopy(changed);bad[1]['condition']=None
save(child,bad,rules)
before=len(request('GET',f'/bom/variants?productId={child}'))
variant(child,values1,status=(400,))
assert len(request('GET',f'/bom/variants?productId={child}'))==before
save(child,changed,rules+[dict(expression=f'{nk} > 8',message='Conflicting restriction')])
variant(child,values1,status=(400,))
# Existing enum and date definitions are reused and validated in configurations.
en=definition('-config-enum',5,2)
dt1=definition('-config-from',4)
dt2=definition('-config-to',4)
textcalc=definition('-config-label',3)
for d in [en,dt1,dt2,textcalc]:
    request('POST',f'/classnodes/{assemblies}/parameters',dict(parameterDefinitionId=d,sortOrder=d,isRequired=False))
save(child,changed,rules+[dict(expression=f'p{en} == 4 AND p{dt2} >= p{dt1}',message='Enum/date restriction')],{f'p{textcalc}':'"p999999"'})
good={**values1,f'p{en}':4,f'p{dt1}':'2026-01-01T03:00:00+03:00',f'p{dt2}':'2026-01-02T00:00:00Z'}
variant(child,good)
for key,val in [(f'p{en}',999999),(f'p{dt1}','2026-01-01T03:00:00'),(f'p{dt2}','2025-01-01T00:00:00Z')]:
    variant(child,{**good,key:val},status=(400,))
# A revision-specific enum subset is a relation, not the entire current catalog.
current_spec=spec(child)
limited=copy.deepcopy(current_spec['parameters'])
next(p for p in limited if p['definitionId']==en)['enumIds']=[4]
request('PUT',f'/bom/products/{child}',dict(expectedRevisionId=current_spec['revision']['id'],unitId=unit,reason='Enum subset',parameters=limited,positions=changed,rules=rules,calculations={}))
variant(child,{**values1,f'p{en}':4})
variant(child,{**values1,f'p{en}':5},status=(400,))
# New template definitions cannot alter the fixed current order lines.
expected_current=totals(v1['id'],3.75,material_class)
assert expected_current==request('GET',f'/bom/orders/{oid}/totals?classId={material_class}')
print('BOM: deep configuration, formulas, modifications, fixed orders, enum subsets, types, cycles and concurrency passed.')
if '--fixture' in sys.argv:
    print(json.dumps(dict(variant=v1['id'],order=oid,materialClass=material_class,expected=expected_current)))
