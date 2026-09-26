"""Read-only check against an API configured with an unavailable database on port 5157."""
import json, urllib.request, urllib.error
try:
    urllib.request.urlopen(urllib.request.Request('http://127.0.0.1:5157/api/products',headers={'Origin':'http://127.0.0.1:4173'}),timeout=10)
except urllib.error.HTTPError as error:
    body=error.read().decode()
    assert error.code==503, (error.code,body)
    assert json.loads(body)['message']=='База данных временно недоступна. Повторите попытку позже.'
    assert error.headers.get('Access-Control-Allow-Origin')=='http://127.0.0.1:4173'
    assert 'Exception' not in body and 'StackTrace' not in body
else:
    raise AssertionError('Expected unavailable database')
print('Unavailable database: 503, safe message, preview CORS passed.')
