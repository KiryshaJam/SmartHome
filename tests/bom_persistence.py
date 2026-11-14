"""Compare the fixture emitted by api_bom.py before/after API/PostgreSQL restart.

python3 tests/bom_persistence.py /tmp/bom-fixture.json --container TEST_CONTAINER --database TEST_DB
Only reads; does not start/stop processes or write to the database.
"""
import argparse
import json
import subprocess
import urllib.request

parser = argparse.ArgumentParser()
parser.add_argument('fixture')
parser.add_argument('--container', required=True)
parser.add_argument('--database', required=True)
parser.add_argument('--base', default='http://127.0.0.1:5156/api')
a = parser.parse_args()
with open(a.fixture) as f:
    fixture = json.load(f)
revision, cls = int(fixture['order']), int(fixture['materialClass'])
with urllib.request.urlopen(f'{a.base}/bom/orders/{revision}/totals?classId={cls}') as r:
    actual = json.load(r)
assert actual == fixture['expected'], (actual, fixture['expected'])
sql = f"SELECT coalesce(jsonb_agg(t),'[]') FROM calc_order_bom({revision},{cls}) t"
result = subprocess.check_output(['docker', 'exec', a.container, 'psql', '-U', 'postgres', '-d', a.database, '-Atc', sql], text=True)
assert json.loads(result) == actual, result
variant = int(fixture['variant'])
with urllib.request.urlopen(f'{a.base}/bom/variants/{variant}/tree?quantity=3') as r:
    tree = json.load(r)
sql = f"SELECT coalesce(jsonb_agg(t),'[]') FROM find_bom({variant},3) t"
result = subprocess.check_output(['docker', 'exec', a.container, 'psql', '-U', 'postgres', '-d', a.database, '-Atc', sql], text=True)
assert json.loads(result) == tree
assert max(t['depth'] for t in tree) >= 1
print('Frozen order totals match the fixture; SQL and API trees/totals match.')
