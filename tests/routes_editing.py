"""Continuation of routes_api.py, on the same disposable fixture database only."""
import os,json,urllib.request,urllib.error,copy
base=os.environ['ROUTES_API'].rstrip('/')+'/api'
def call(method,path,data=None,status=200):
 r=urllib.request.Request(base+path,method=method,data=json.dumps(data).encode() if data is not None else None,headers={'Content-Type':'application/json'})
 try:
  with urllib.request.urlopen(r) as f: code=f.status;body=f.read()
 except urllib.error.HTTPError as e: code=e.code;body=e.read()
 assert code==status,(path,code,body)
 return json.loads(body) if body else None
rows=call('GET','/routes')
t=next(x for x in rows if x['name']=='Типовой монтаж комплекта')
a=next(x for x in rows if x['name']=='Проводной 1 м')
doc=call('GET',f"/routes/{t['id']}")
original={**doc['setting'],'steps':copy.deepcopy(doc['steps']),'inputs':copy.deepcopy(doc['inputs'])}
modified=copy.deepcopy(original)
new=copy.deepcopy(next(x for x in modified['steps'] if x['id']==t['finalStepId']))
new.update(id=-1,number=40,resultName='Контроль после комплектации',setupMinutes=1,unitMinutes=0,hourlyRate=0)
modified['steps'].append(new)
modified['inputs'].append(dict(stepId=-1,sourceId=t['finalStepId'],amount=1,forQuantity=1,parameterId=None))
modified['finalStepId']=-1
updated=call('PUT',f"/routes/{t['id']}",modified)
assert updated['setting']['finalStepId']!=t['finalStepId']
assert call('GET',f"/routes/{a['id']}")['setting']['finalStepId']==updated['setting']['finalStepId']
c=call('GET',f"/routes/{a['id']}/calculate")
assert c['minutes']==31 and c['total']==510
call('PUT',f"/routes/{t['id']}",original)
assert call('GET',f"/routes/{a['id']}/calculate")['minutes']==30
# An owned standalone source remains editable when another route consumes its result.
child=next(x for x in rows if x['name']=='Самостоятельный для другого изделия')
childdoc=call('GET',f"/routes/{child['id']}")
payload={**childdoc['setting'],'steps':childdoc['steps'],'inputs':childdoc['inputs']}
payload['steps'][-1]['tooling']='Оснастка исходного узла'
call('PUT',f"/routes/{child['id']}",payload)
assert call('GET',f"/routes/{a['id']}/calculate")['total']==510
# Internal positions of a foreign graph cannot be taken over as external roots.
parent=next(x for x in rows if x['name']=='Маршрут с вложенным узлом')
pd=call('GET',f"/routes/{parent['id']}");owned=set(pd['editableStepIds'])
payload={**pd['setting'],'steps':[x for x in pd['steps'] if x['id'] in owned],'inputs':[x for x in pd['inputs'] if x['stepId'] in owned]}
payload['inputs'][0]['sourceId']=next(x['id'] for x in doc['steps'] if x['kind']=='purchase')
call('PUT',f"/routes/{parent['id']}",payload,400)
print('Template structural editing, application relinking, standalone nested editing and external node protection passed.')
