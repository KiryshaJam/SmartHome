"""Apply old schema, freeze history, upgrade, and test normalized SQL invariants.

Generate EF baseline (0 -> AddBom) and upgrade (AddBom -> NormalizeBomOrderLines)
scripts into .local first. Supply a NEW EMPTY disposable DB, never main/playground.
No database/container creation or deletion is performed by this runner.
"""
import argparse
import pathlib
import subprocess

p = argparse.ArgumentParser()
p.add_argument('--container', required=True)
p.add_argument('--database', required=True)
p.add_argument('--baseline-sql', type=pathlib.Path, required=True)
p.add_argument('--upgrade-sql', type=pathlib.Path, required=True)
p.add_argument('--allow-test-writes', action='store_true')
p.add_argument('--migration-only', action='store_true', help='Apply baseline, fixture and upgrade; run SQL invariants separately.')
a = p.parse_args()
if not a.allow_test_writes or not a.database.startswith(('smarthome_test_', 'smarthome_regression_')):
    p.error('Use --allow-test-writes and a NEW smarthome_test_* or smarthome_regression_* database.')
cmd = ['docker', 'exec', '-i', a.container, 'psql', '-X', '-U', 'postgres', '-d', a.database, '-v', 'ON_ERROR_STOP=1']
def sql(text):
    return subprocess.check_output(cmd + ['-At'], input=text, text=True)
identity = sql("SELECT current_database(), (SELECT count(*) FROM pg_tables WHERE schemaname='public');")
if identity.strip() != a.database + '|0':
    raise SystemExit('Refusing writes: target database is not the named empty disposable DB.')
root = pathlib.Path(__file__).resolve().parent
paths = [a.baseline_sql, root / 'order_lines_upgrade_before.sql', a.upgrade_sql]
if not a.migration_only:
    paths.append(root / 'order_lines.sql')
for path in paths:
    sql(path.read_text())
    print('Passed:', path.name, flush=True)
if a.migration_only:
    print('Upgrade applied; SQL invariants have not run yet.', flush=True)
else:
    print('Upgrade preserved every history record, quantity, row order and frozen total; SQL invariants passed.', flush=True)
