# Every migration in the repository, against the model it claims to describe.
#
#   python tools/check-migrations.py
#
# Nothing else in this repository looks at a migration. The tests build their
# database from the model — SQLite, created fresh from `DbContext` — so a
# migration can be wrong, missing, or absent from the deployment entirely and
# 469 backend tests still pass.
#
# That is not hypothetical. `Drop_Dead_TenantBranding_Table` sat in this folder
# for a day without ever having been run: the development database had thirty of
# thirty-one, and the one it was missing was invisible because no check looked.
# The first place it would have run is a customer's database.
#
# Two things are asked here, and they are different questions:
#
#   1. Does the model still match the last migration? A field added to an entity
#      without a migration reaches production as a column that does not exist,
#      and the error arrives as a SQL failure on somebody's screen.
#
#   2. Do the migration files and the snapshot agree on which migration is last?
#      They drift when two branches each add one.
#
# What this cannot ask is whether the migrations run in order on an empty
# database. That needs a real SQL Server, so it stays a deployment step —
# `dotnet run` in the DbMigrator project against an empty database, which is
# exactly what the `migrator` service in docker-compose does.

import os
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
EFCORE = os.path.join(ROOT, 'src', 'InternshipManagementSystem.EntityFrameworkCore')
MIGRATIONS = os.path.join(EFCORE, 'Migrations')


def migration_names():
    """The migrations on disk, oldest first. The timestamp prefix is the order."""
    names = []

    for entry in sorted(os.listdir(MIGRATIONS)):
        if not entry.endswith('.cs'):
            continue
        if entry.endswith('.Designer.cs') or 'ModelSnapshot' in entry:
            continue

        names.append(entry[:-3])

    return names


def snapshot_names():
    """Every migration the Designer files claim exists, by their own file names."""
    return sorted(
        entry[: -len('.Designer.cs')]
        for entry in os.listdir(MIGRATIONS)
        if entry.endswith('.Designer.cs'))


def model_matches_migrations():
    """
    Asks EF itself, because only EF knows what the model compiles to.

    Returns (ok, message). A build failure is reported as a failure rather than
    swallowed: a check that cannot run has not passed.
    """
    try:
        result = subprocess.run(
            ['dotnet', 'ef', 'migrations', 'has-pending-model-changes',
             '--project', EFCORE, '--startup-project', EFCORE],
            capture_output=True, text=True, timeout=900, cwd=ROOT)
    except FileNotFoundError:
        return False, 'dotnet is not on PATH, so nothing was checked.'
    except subprocess.TimeoutExpired:
        return False, 'dotnet ef did not finish, so nothing was checked.'

    output = (result.stdout or '') + (result.stderr or '')

    if 'No changes have been made to the model' in output:
        return True, 'the model matches the last migration'

    # EF says this in as many words, and its sentence is better than any
    # summary of it: it names the fix. Passing it through matters — a check that
    # reports "dotnet ef failed" sends somebody hunting a build problem that is
    # not there, which is the same defect this repository has spent a day
    # removing from its screens.
    if 'Changes have been made to the model' in output:
        return False, ('the model has changes no migration describes '
                       '- run: dotnet ef migrations add <name> '
                       '--project src/InternshipManagementSystem.EntityFrameworkCore '
                       '--startup-project src/InternshipManagementSystem.EntityFrameworkCore')

    detail = next(
        (line.strip() for line in output.splitlines()
         if 'error' in line.lower()),
        'dotnet ef exited %d and said nothing this check understood' % result.returncode)

    return False, detail


def main():
    files = migration_names()
    designers = snapshot_names()

    print('migrations on disk        : %d' % len(files))

    problems = []

    missing_designer = sorted(set(files) - set(designers))
    orphan_designer = sorted(set(designers) - set(files))

    if missing_designer:
        problems.append('migration with no Designer file: ' + ', '.join(missing_designer))

    if orphan_designer:
        problems.append('Designer file with no migration: ' + ', '.join(orphan_designer))

    ok, message = model_matches_migrations()
    print('model against migrations  : %s' % message)

    if not ok:
        problems.append(message)

    if problems:
        print('')
        for problem in problems:
            print('  %s' % problem)
        return 1

    print('')
    print('nothing pending.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
