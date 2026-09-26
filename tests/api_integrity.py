from api_regression import *
import api_regression

# Draft lifecycle, required values, move mapping and rejected changes.
required = request('POST', f'/classnodes/{root}/parameters', dict(parameterDefinitionId=definition('-required', 2),sortOrder=10,isRequired=True,minNumberValue=1,maxNumberValue=10))
leaf_params = request('GET',f'/classnodes/{leaf}/parameters')
required_leaf = next(p for p in leaf_params if p['parameterDefinitionId']==required['parameterDefinitionId'])['id']
request('PUT',f'/products/{product}/status',{'isDraft':False},expected=(400,))
request('PUT',f'/products/{product}/parameters/{required_leaf}',{'numberValue':9})
request('PUT',f'/products/{product}/status',{'isDraft':False})
request('DELETE',f'/products/{product}/parameters/{required_leaf}',expected=(400,))
request('PUT',f'/products/{product}/parameters/{required_leaf}',{'numberValue':999},expected=(400,))
request('PUT',f'/classnodes/{leaf}/parameters/{required_leaf}',{'sortOrder':10,'isRequired':True,'minNumberValue':1,'maxNumberValue':8},expected=(400,))
sibling = node('-sibling',child,True)
request('PUT',f'/products/{product}/move',{'newClassNodeId':sibling})
card = request('GET',f'/products/{product}/parameters/card')
assert len(card['parameters'])==6 and all(p['id'] for p in card['parameters'])
request('PUT',f'/products/{product}/move',{'newClassNodeId':foreign},expected=(400,))
assert request('GET',f'/products/{product}')['classNodeId']==sibling
request('PUT',f'/products/{product}/move',{'newClassNodeId':leaf})
request('POST','/classnodes',dict(name=prefix+'-invalid',shortName=prefix+'-invalid',parentId=leaf,isTerminal=True,sortOrder=1),expected=(400,))
request('POST','/parameter-definitions',dict(name=prefix+'-badtype',shortName=prefix+'-badtype',valueType=99),expected=(400,))
request('POST','/enum-classes',dict(name=prefix+'-bad-enum',shortName=prefix+'-bad-enum',valueType=99),expected=(400,))
string_cp=next(p['id'] for p in leaf_params if p['valueType']==3)
date_cp=next(p['id'] for p in leaf_params if p['valueType']==4)
request('PUT',f'/products/{product}/parameters/{string_cp}',{'stringValue':'x'*1024})
request('PUT',f'/products/{product}/parameters/{string_cp}',{'stringValue':'x'*1025},expected=(400,))
request('PUT',f'/products/{product}/parameters/{date_cp}',{'dateTimeValue':'2026-01-01T03:00:00+03:00'})
request('PUT',f'/products/{product}/parameters/{date_cp}',{'dateTimeValue':'2026-01-01T03:00:00'},expected=(400,))
request('PUT',f'/products/{product}/parameters/{date_cp}',{'dateTimeValue':'bad-date'},expected=(400,))
request('DELETE','/enum-classes/2/values/4',expected=(409,))
request('DELETE','/enum-classes/2',expected=(409,))
request('GET','/products/2147483647',expected=(404,))
print(f'Passed {api_regression.checks} API requests including integrity and error contracts.')
