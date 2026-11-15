"""Run after routes_api.py against the same new disposable DB; ROUTES_API is required."""
import json, os, urllib.request, urllib.error, uuid
from concurrent.futures import ThreadPoolExecutor

base = os.environ['ROUTES_API'].rstrip('/') + '/api'
prefix = 'resources-' + uuid.uuid4().hex[:8]

def request(method, path, body=None, expected=200):
    req = urllib.request.Request(base + path, method=method,
        data=json.dumps(body).encode() if body is not None else None,
        headers={'Content-Type': 'application/json'})
    try:
        with urllib.request.urlopen(req) as response:
            status, content = response.status, response.read()
    except urllib.error.HTTPError as error:
        status, content = error.code, error.read()
    assert status == expected, (path, status, content.decode())
    return json.loads(content) if content else None

look = request('GET', '/routes/lookups')
roots = request('GET', '/classnodes/roots')
def create_type(branch, label):
    root = next(x for x in roots if x['shortName'] == branch)
    return request('POST', '/classnodes', dict(name=prefix+label, shortName=prefix+label,
        parentId=root['id'], isTerminal=True, sortOrder=0), 201)['id']

dtype = create_type('route_departments', '-department-type')
ctype = create_type('route_centers', '-center-type')
dep = request('POST', '/routes/departments', dict(name='  '+prefix+' участок  ', typeId=dtype))
assert dep['name'] == prefix+' участок'
center = request('POST', '/routes/work-centers', dict(name=prefix+' пост', typeId=ctype, departmentId=dep['id']))
look = request('GET', '/routes/lookups')
assert any(x['id'] == dep['id'] and x['typeId'] == dtype for x in look['departments'])
assert any(x['id'] == center['id'] and x['department'] == dep['name'] and x['departmentId'] == dep['id'] for x in look['centers'])
assert any(x['id'] == dtype for x in look['departmentTypes'])
assert any(x['id'] == ctype for x in look['centerTypes'])
counts = (len(look['departments']), len(look['centers']))
for data in [dict(name=' ',typeId=dtype),dict(name='x',typeId=ctype),dict(name='x',typeId=2147483647),dict(name='x',typeId=next(x['id'] for x in roots if x['shortName']=='route_departments')),dict(name='x'*257,typeId=dtype)]:
    request('POST','/routes/departments',data,400)
request('POST','/routes/departments',dict(name=dep['name'].upper(),typeId=dtype),400)
for data in [dict(name='x',typeId=ctype,departmentId=2147483647),dict(name='x',typeId=dtype,departmentId=dep['id']),dict(name=center['name'].upper(),typeId=ctype,departmentId=dep['id'])]:
    request('POST','/routes/work-centers',data,400)
look = request('GET', '/routes/lookups')
assert counts == (len(look['departments']),len(look['centers']))
# Duplicate checking and insertion share the route transaction lock.
def concurrent_create(_):
    req=urllib.request.Request(base+'/routes/departments',method='POST',headers={'Content-Type':'application/json'},data=json.dumps(dict(name=prefix+' concurrent',typeId=dtype)).encode())
    try:
        with urllib.request.urlopen(req) as r:return r.status
    except urllib.error.HTTPError as e:return e.code
with ThreadPoolExecutor(max_workers=2) as pool: assert sorted(pool.map(concurrent_create,range(2))) == [200,400]
# New centers survive route saving and do not change material/labor calculations.
rows = request('GET','/routes')
template = next(x for x in rows if x['name']=='Типовой монтаж комплекта')
a = next(x for x in rows if x['name']=='Проводной 1 м')
doc = request('GET',f"/routes/{template['id']}")
for step in doc['steps']:
    if step['kind']=='production': step['workCenterId']=center['id']
request('PUT',f"/routes/{template['id']}",{**doc['setting'],'steps':doc['steps'],'inputs':doc['inputs']})
loaded=request('GET',f"/routes/{template['id']}")
assert all(x['workCenterId']==center['id'] for x in loaded['steps'] if x['kind']=='production')
assert request('GET',f"/routes/{a['id']}/calculate?quantity=1")['total']==510
assert request('GET',f"/routes/{a['id']}/calculate?quantity=10")['total']==3813
print('Resources: new classifier types, department/center creation, reload, invalid types, missing parents, blanks, duplicate/concurrent requests and route calculation passed.')
