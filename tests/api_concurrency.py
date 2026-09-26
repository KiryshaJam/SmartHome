from api_regression import *
from concurrent.futures import ThreadPoolExecutor
import threading

def race(actions):
    barrier = threading.Barrier(len(actions))
    def run(action):
        barrier.wait()
        return action()
    with ThreadPoolExecutor(max_workers=len(actions)) as pool:
        return list(pool.map(run, actions))

# Two writes to the same initially absent value: a conflict is acceptable, never 500.
d = definition('-concurrent',2)
cp = request('POST',f'/classnodes/{leaf}/parameters',dict(parameterDefinitionId=d,sortOrder=20,isRequired=False))['id']
race([lambda: request('PUT',f'/products/{product}/parameters/{cp}',{'numberValue':3},expected=(200,409)),lambda: request('PUT',f'/products/{product}/parameters/{cp}',{'numberValue':4},expected=(200,409))])
card = request('GET',f'/products/{product}/parameters/card')
assert next(p for p in card['parameters'] if p['classParameterId']==cp)['numberValue'] in (3,4)
# Opposite concurrent moves cannot form a cycle.
a,b=node('-race-a'),node('-race-b')
race([lambda: request('PUT',f'/classnodes/{a}/move',{'newParentId':b},expected=(200,400,409)),lambda: request('PUT',f'/classnodes/{b}/move',{'newParentId':a},expected=(200,400,409))])
assert not(request('GET',f'/classnodes/{a}')['parentId']==b and request('GET',f'/classnodes/{b}')['parentId']==a)
# A late required binding must roll back parent and descendant inserts atomically.
request('PUT',f'/products/{product}/status',{'isDraft':False})
d = definition('-atomic',3)
request('POST',f'/classnodes/{root}/parameters',dict(parameterDefinitionId=d,sortOrder=30,isRequired=True),expected=(400,))
for c in (root,child,leaf):
    assert all(p['parameterDefinitionId'] != d for p in request('GET',f'/classnodes/{c}/parameters'))
print('Concurrent writes, opposite tree moves and atomic inheritance passed.')
