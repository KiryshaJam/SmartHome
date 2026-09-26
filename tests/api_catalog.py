from api_regression import *
import api_regression
# Read collections, single records, tree projections and missing-object contracts.
for path in ['/classnodes','/classnodes/roots','/classnodes/leaves','/products','/parameter-definitions','/parameter-groups','/measure-units','/enum-classes']:
    request('GET',path)
for suffix in ['', '/children','/parents','/descendants','/leaves']:
    request('GET',f'/classnodes/{root}'+suffix)
    request('GET','/classnodes/2147483647'+suffix,expected=(404,))
for base in ['/products','/parameter-definitions','/parameter-groups','/measure-units','/enum-classes']:
    request('GET',base+'/1')
    request('GET',base+'/2147483647',expected=(404,))
for suffix in ['', '/with-descendants']:
    request('GET',f'/product-search/by-class/{root}'+suffix)
request('POST','/product-search/filter',{'classNodeId':leaf})
request('PUT',f'/classnodes/{root}/sort-order',{'sortOrder':4})
request('POST',f'/classnodes/{leaf}/parameters/copy-from-parent',{})
parent_cp=request('GET',f'/classnodes/{root}/parameters')[0]
request('POST',f'/classnodes/{root}/parameters/{parent_cp["id"]}/copy-to-descendants',{})
request('GET',f'/classnodes/{root}/parameters/{parent_cp["id"]}')
# Free and used bindings; local settings survive repeated copying.
d = definition('-delete-binding',2)
cp=request('POST',f'/classnodes/{root}/parameters',dict(parameterDefinitionId=d,sortOrder=10,isRequired=False))['id']
request('DELETE',f'/classnodes/{root}/parameters/{cp}')
assert all(p['parameterDefinitionId']!=d for p in request('GET',f'/classnodes/{leaf}/parameters'))
request('DELETE',f'/parameter-definitions/{d}')
request('DELETE',f'/classnodes/{root}/parameters/{parent_cp["id"]}',expected=(400,))
# Enumerations: all types, duplicate values, atomic reorder, free deletion.
for kind in (1,2,3):
    enum=request('POST','/enum-classes',dict(name=prefix+'-enum'+str(kind),shortName=prefix+'-enum'+str(kind),valueType=kind,sortOrder=3,measureUnitId=5 if kind==2 else None))['id']
    field={1:'stringValue',2:'numberValue',3:'iconValue'}[kind]
    path=f'/enum-classes/{enum}/values'
    first=request('POST',path,{field:12 if kind==2 else 'Alpha','sortOrder':1})['id']
    second=request('POST',path,{field:15 if kind==2 else 'Beta','sortOrder':2})['id']
    request('PUT',path+f'/{first}/sort-order',{'sortOrder':2})
    assert request('GET',path+f'/{second}')['sortOrder']==1
    request('POST',path,{field:12 if kind==2 else 'Alpha','sortOrder':3},expected=(400,409))
    request('PUT',f'/enum-classes/{enum}/sort-order',{'sortOrder':4})
    request('DELETE',path+f'/{first}')
    request('DELETE',f'/enum-classes/{enum}')
g=request('POST','/parameter-groups',dict(name=prefix+'-group',shortName=prefix+'-group',sortOrder=1))['id']
request('PUT',f'/parameter-groups/{g}/sort-order',{'sortOrder':3})
request('DELETE',f'/parameter-groups/{g}')
request('POST','/measure-units',dict(name=prefix+'-unit',shortName=prefix+'-unit'))
request('DELETE',f'/products/{product}')
request('DELETE',f'/classnodes/{leaf}')
print(f'Passed {api_regression.checks} requests including catalog CRUD and ordering.')
