"""Run only against a disposable database/API: python3 tests/api_regression.py --allow-test-writes."""
import json, sys, urllib.request, urllib.error, uuid
BASE = 'http://127.0.0.1:5156/api'
if '--allow-test-writes' not in sys.argv:
    sys.exit('Explicit --allow-test-writes is required; point API to a disposable database first.')
prefix = 'test-' + uuid.uuid4().hex[:10]
checks = 0

def request(method, path, body=None, expected=(200, 201, 204)):
    global checks
    req = urllib.request.Request(BASE + path, data=None if body is None else json.dumps(body).encode(), method=method, headers={'Content-Type': 'application/json'})
    try:
        with urllib.request.urlopen(req) as response:
            status, data = response.status, response.read()
    except urllib.error.HTTPError as error:
        status, data = error.code, error.read()
    assert status in expected, (method, path, status, data.decode()[:1500])
    checks += 1
    return json.loads(data) if data and data[:1] in (b'[', b'{') else None

def node(label, parent=None, terminal=False):
    return request('POST','/classnodes',dict(name=prefix+label,shortName=prefix+label,parentId=parent,isTerminal=terminal,sortOrder=0))['id']

def definition(label, kind, enum=None):
    return request('POST','/parameter-definitions',dict(name=prefix+label,shortName=prefix+label,valueType=kind,enumClassId=enum))['id']

root = node('-root')
child = node('-child', root)
leaf = node('-leaf', child, True)
foreign = node('-foreign', None, True)
for kind, value, filter_value in [(1, {'integerValue': 9}, {'integerValue': 9}), (2, {'numberValue': 9}, {'numberFrom': 8, 'numberTo': 10}), (3, {'stringValue': 'test string'}, {'stringContains': 'test'}), (4, {'dateTimeValue': '2026-01-01T00:00:00Z'}, {'dateFrom': '2025-01-01T00:00:00Z'}), (5, {'enumValueId': 4}, {'enumValueId': 4})]:
    d = definition('-p'+str(kind), kind, 2 if kind == 5 else None)
    cp = request('POST', f'/classnodes/{root}/parameters', dict(parameterDefinitionId=d,sortOrder=kind,isRequired=False))
    leaf_cp = next(p for p in request('GET',f'/classnodes/{leaf}/parameters') if p['parameterDefinitionId']==d)
    assert leaf_cp['isInherited']
    if kind == 1:
        product = request('POST','/products',dict(name=prefix+'-device',shortName=prefix+'-device',classNodeId=leaf))['id']
    request('PUT',f'/products/{product}/parameters/{leaf_cp["id"]}', value)
    matches = request('POST','/product-search/filter-with-descendants',dict(classNodeId=root,classParameterId=cp['id'],**filter_value))
    assert product in [p['id'] for p in matches]
    request('POST','/product-search/filter-with-descendants',dict(classNodeId=foreign,classParameterId=cp['id'],**filter_value),expected=(400,))
    card = request('GET',f'/products/{product}/parameters/card')
    assert all(p['isInherited'] for p in card['parameters'])
print(f'Passed {checks} API requests; subtree search verified for all five types.')
