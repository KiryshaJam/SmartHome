"""Run only against a freshly restored disposable database; ROUTES_API is required."""
import os,json,urllib.request,urllib.error,copy
BASE=os.environ['ROUTES_API'].rstrip('/')+'/api'
def req(method,path,data=None,status=200):
    r=urllib.request.Request(BASE+path,data=None if data is None else json.dumps(data).encode(),method=method,headers={'Content-Type':'application/json'})
    try:
        with urllib.request.urlopen(r) as f: code=f.status; body=f.read()
    except urllib.error.HTTPError as e: code=e.code; body=e.read()
    assert code==status or (status==200 and code==201),(method,path,code,body.decode())
    return json.loads(body) if body else None
l=req('GET','/routes/lookups'); products={x['name']:x for x in l['products']}
p=products['Комплект умного дома']; cable=products['Кабель сигнальный']; fast=products['Крепёж монтажный']
variant=next(x for x in l['variants'] if x['name']=='Проводной — 2 датчика')
par=next((x['id'] for x in req('GET','/parameter-definitions') if x['shortName']=='route_extra_cable'),None)
if par is None: par=req('POST','/parameter-definitions',dict(name='Длина дополнительного кабеля',shortName='route_extra_cable',valueType=2,measureUnitId=cable['unitId']))['id']
for product,price in [(cable,40),(fast,12)]: req('PUT',f"/products/{product['id']}/price",{'purchasePrice':price})
def step(i,prod,name,number=0,setup=0,minutes=0,rate=0):
    return dict(id=i,kind='production' if number else 'purchase',productId=prod['id'],unitId=prod['unitId'],resultName=name,number=number,setupMinutes=setup,unitMinutes=minutes,hourlyRate=rate,tooling='Тестер / отвёртка',operationId=l['operations'][0]['id'] if number else None,professionId=l['professions'][0]['id'] if number else None,workCenterId=l['centers'][0]['id'] if number else None,qualificationId=l['qualifications'][0]['id'] if number else None)
def edge(a,b,n=1,d=1,param=None): return dict(stepId=a,sourceId=b,amount=n,forQuantity=d,parameterId=param)
body=dict(name='Типовой монтаж комплекта',productId=p['id'],kind='template',parameterId=par,parameterValue=1,finalStepId=-5,steps=[step(-1,cable,'Кабель'),step(-2,fast,'Крепёж'),step(-3,p,'Подготовить линии',10,5,3,540),step(-4,p,'Уложить и закрепить линии',20,6,8,540),step(-5,p,'Проверить и укомплектовать',30,4,4,660)],inputs=[edge(-3,-1,4),edge(-4,-3),edge(-4,-2,2),edge(-5,-4),edge(-5,-1,param=par)])
t=req('POST','/routes',body);tid=t['setting']['id']
a=req('POST',f'/routes/{tid}/apply',dict(name='Проводной 1 м',productId=p['id'],revisionId=variant['id'],parameterValue=1));aid=a['setting']['id']
b=req('POST',f'/routes/{tid}/apply',dict(name='Проводной 2 м',productId=p['id'],revisionId=variant['id'],parameterValue=2));bid=b['setting']['id']
assert a['setting']['finalStepId']==b['setting']['finalStepId']==t['setting']['finalStepId']
results=[]
for n,material,mins,labor,total in [(1,224,30,286,510),(10,2240,165,1573,3813)]:
    c=req('GET',f'/routes/{aid}/calculate?quantity={n}');assert (c['materialCost'],c['minutes'],c['laborCost'],c['total'])==(material,mins,labor,total),c
    results.append(c)
assert req('GET',f'/routes/{bid}/calculate')['total']==550
assert req('GET',f'/routes/{aid}/calculate')['total']==510
# Selected execution agrees with BOM consumption for cable and fasteners.
bom=req('GET',f"/bom/variants/{variant['id']}/totals")
for product,norm in [(cable,5),(fast,2)]: assert next(x['required_quantity'] for x in bom if x['product_id']==product['id'])==norm
# Missing and zero prices remain distinct.
req('PUT',f"/products/{cable['id']}/price",{'purchasePrice':None}); c=req('GET',f'/routes/{aid}/calculate');assert c['total'] is None and c['minutes']==30 and 'Кабель сигнальный' in c['missingPrices']
req('PUT',f"/products/{cable['id']}/price",{'purchasePrice':0});assert req('GET',f'/routes/{aid}/calculate')['total']==310
req('PUT',f"/products/{cable['id']}/price",{'purchasePrice':-1},400)
req('PUT',f"/products/{cable['id']}/price",{'purchasePrice':40})
req('GET',f'/routes/{aid}/calculate?quantity=0',status=400)
# Graph errors roll back the entire transaction.
for mutation in ['cycle','unit','denominator','revision']:
    bad=copy.deepcopy(body);bad['name']='Invalid '+mutation
    if mutation=='cycle': bad['inputs'].append(edge(-3,-5))
    if mutation=='unit': bad['steps'][0]['unitId']=p['unitId']
    if mutation=='denominator':bad['inputs'][0]['forQuantity']=0
    if mutation=='revision':bad['revisionId']=next(x['id'] for x in l['variants'] if x['productId']!=p['id'])
    req('POST','/routes',bad,400)
assert len(req('GET','/routes'))==3
# Shared intermediate step: setup once after demands are merged, fractional norms.
shared=copy.deepcopy(body);shared.update(name='Ветвление',kind='standalone',parameterId=None,parameterValue=None)
shared['inputs'][-1]=edge(-5,-3,0.5)
s=req('POST','/routes',shared); c=req('GET',f"/routes/{s['setting']['id']}/calculate")
first=next(x for x in c['labor'] if x['number']==10); assert first['required_quantity']==1.5 and first['minutes']==9.5
# Editing and reopening a different product with no named execution.
other=products['Комплект усиленный']; independent=copy.deepcopy(body);independent.update(name='Самостоятельный для другого изделия',productId=other['id'],kind='standalone',parameterId=None,parameterValue=None)
for x in independent['steps']:
    if x['kind']=='production':x['productId']=other['id'];x['unitId']=other['unitId']
independent['inputs'][-1]['parameterId']=None
saved=req('POST','/routes',independent); ss=saved['setting'];payload={**ss,'steps':saved['steps'],'inputs':saved['inputs']};payload['steps'][-1]['tooling']='Новый тестер'
req('PUT',f"/routes/{ss['id']}",payload); assert req('GET',f"/routes/{ss['id']}")['setting']['revisionId'] is None
# Applications accept parameter updates only.
req('PUT',f'/routes/{bid}',dict(parameterValue=3));assert req('GET',f'/routes/{bid}/calculate')['total']==590
req('PUT',f'/routes/{bid}',dict(parameterValue=2));assert req('GET',f'/routes/{aid}/calculate')['total']==510
# Invalid service classes cannot hold products.
req('POST','/products',dict(name='Invalid service product',shortName='invalid_service',classNodeId=l['operations'][0]['id']),400)
# Missing template values are rejected without damaging existing settings.
req('PUT',f'/routes/{bid}',dict(parameterValue=None),400)
assert req('GET',f'/routes/{bid}/calculate')['total']==550
# Reuse a manufactured nested unit without charging its arbitrary catalog price.
child=ss
req('PUT',f"/products/{other['id']}/price",{'purchasePrice':99999})
parent=dict(name='Маршрут с вложенным узлом',productId=p['id'],kind='standalone',finalStepId=-1,steps=[step(-1,p,'Укомплектовать вложенный узел',40,1,2,600)],inputs=[edge(-1,child['finalStepId'],2)])
parent=req('POST','/routes',parent); ps=parent['setting'];calc=req('GET',f"/routes/{ps['id']}/calculate")
# The nested route has the same 5m cable and 2 fasteners per unit.
assert calc['materialCost']==448 and calc['total']<99999
assert child['finalStepId'] not in parent['editableStepIds']
attack={**ps,'steps':parent['steps'],'inputs':parent['inputs']}
req('PUT',f"/routes/{ps['id']}",attack,400)
owned=set(parent['editableStepIds']); payload={**ps,'steps':[x for x in parent['steps'] if x['id'] in owned],'inputs':[x for x in parent['inputs'] if x['stepId'] in owned]}
payload['steps'][0]['tooling']='Оснастка родителя'; req('PUT',f"/routes/{ps['id']}",payload)
assert req('GET',f"/routes/{child['id']}/calculate")['total']==510
req('PUT',f"/products/{other['id']}/price",{'purchasePrice':None})
print(json.dumps(dict(templateId=tid,settingId=aid,secondId=bid,results=results),ensure_ascii=False,indent=2))
