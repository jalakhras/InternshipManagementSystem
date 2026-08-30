# Every screen, route, link and button — and whether each one leads anywhere.
#
#   python tools/check-reachability.py
#
# The defect this exists for has a shape, and this project has produced it six
# times: a finished application service with no route and no screen, a screen
# with a button bound to nothing, a nav entry pointing at a route that does not
# exist. Each one reads as DONE in any inventory that counts services rather
# than journeys, and each one is discovered by a person clicking it.
#
# What it checks, and each is a thing that has actually gone wrong here:
#
#   1. Every nav entry points at a route that exists.
#   2. Every routerLink in a template points at a route that exists.
#   3. Every (click) handler names a method the component actually has.
#   4. Every component a route names can be found on disk.
#   5. Every permission a guard or a permissionSignal asks for is defined —
#      as one of this product's, or one of ABP's own.
#
# It reads the source rather than running the app, so it is fast and needs
# nothing running. That is also its limit: it cannot tell whether a handler that
# exists does anything worth doing, and it says so rather than implying more.

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
APP = os.path.join(ROOT, 'angular', 'src', 'app')


def read(path):
    with io.open(path, encoding='utf-8') as handle:
        return handle.read()


def walk(base, *suffixes):
    for here, dirs, files in os.walk(base):
        dirs[:] = [d for d in dirs if d != 'node_modules']

        for name in files:
            if name.endswith(suffixes):
                yield os.path.join(here, name)


# --- routes ------------------------------------------------------------------

PATH = re.compile(r"path:\s*'([^']*)'")
LOADS = re.compile(r"import\('([^']+)'\)")


def routes():
    """Every path a route file declares, and the files they load."""
    declared, loaded = set(), []

    for path in walk(APP, '.routes.ts'):
        text = read(path)

        for match in PATH.finditer(text):
            declared.add(match.group(1))

        for match in LOADS.finditer(text):
            loaded.append((path, match.group(1)))

    # app.routes.ts nests children under a parent path, and the top level is
    # what a link is written against. Both halves are collected flat: this is a
    # reachability check, not a router.
    return declared, loaded


# --- what the templates ask for ----------------------------------------------

ROUTER_LINK = re.compile(r"""routerLink="/([a-zA-Z0-9\-/]*)\"""")
ROUTER_LINK_BOUND = re.compile(r"""\[routerLink\]="\['/([a-zA-Z0-9\-]*)'""")
CLICK = re.compile(r"""\(click\)="([a-zA-Z_$][a-zA-Z0-9_$]*)\(""")
NAV_ROUTE = re.compile(r"route:\s*'/([a-zA-Z0-9\-/]*)'")


def first_segment(link):
    return link.split('/')[0]


def check_links(declared):
    problems = []
    seen = set()

    for path in walk(APP, '.html'):
        text = read(path)

        for match in list(ROUTER_LINK.finditer(text)) + list(ROUTER_LINK_BOUND.finditer(text)):
            segment = first_segment(match.group(1))

            if not segment or segment in declared or segment in seen:
                continue

            seen.add(segment)
            problems.append(('dead link', os.path.relpath(path, ROOT), '/' + segment))

    nav = os.path.join(APP, 'core', 'navigation.ts')

    if os.path.exists(nav):
        for match in NAV_ROUTE.finditer(read(nav)):
            segment = first_segment(match.group(1))

            if segment and segment not in declared:
                problems.append(('dead nav entry', 'core/navigation.ts', '/' + segment))

    return problems


def check_handlers():
    """A (click) that names a method the component does not have."""
    problems = []

    for template in walk(APP, '.component.html'):
        component = template[: -len('.html')] + '.ts'

        if not os.path.exists(component):
            continue

        code = read(component)
        text = read(template)

        for match in CLICK.finditer(text):
            name = match.group(1)

            # Signals are called too — `open.set(...)` — and a handler may live
            # on a signal or be inherited. Only a bare name with no member
            # access is checked, which is the shape a missing handler takes.
            if name in ('t',):
                continue

            declared = (
                re.search(r'\b%s\s*[(:<]' % re.escape(name), code)
                or re.search(r'\b%s\s*=' % re.escape(name), code)
            )

            if not declared:
                problems.append(('handler not found', os.path.relpath(template, ROOT), name + '()'))

    return problems


def check_components(loaded):
    problems = []

    for source, target in loaded:
        if not target.startswith('.'):
            continue

        resolved = os.path.normpath(os.path.join(os.path.dirname(source), target))

        if not (os.path.exists(resolved + '.ts') or os.path.exists(resolved)):
            problems.append(('component missing', os.path.relpath(source, ROOT), target))

    return problems


# --- permissions -------------------------------------------------------------

POLICY = re.compile(r"requiredPolicy:\s*'([^']+)'")
SIGNAL = re.compile(r"permissionSignal\(\s*'([^']+)'\s*\)")
NAV_PERM = re.compile(r"permission:\s*'([^']+)'")


def check_permissions():
    """A guard asking for a policy nothing defines."""
    known = set(re.findall(r"'((?:Assessment|Abp)[A-Za-z.]+)'",
                           read(os.path.join(APP, 'core', 'permissions.ts'))))

    problems, seen = [], set()

    for path in walk(APP, '.ts'):
        text = read(path)

        for match in list(POLICY.finditer(text)) + list(SIGNAL.finditer(text)) + list(NAV_PERM.finditer(text)):
            name = match.group(1)

            if name in known or name in seen:
                continue

            # ABP's own permissions are defined by its modules, not by this
            # product's constants, and naming them directly is deliberate — the
            # identity module owns roles, so inventing a second name for the same
            # authority is how two guards end up disagreeing.
            if name.startswith('AbpIdentity.') or name.startswith('AbpTenantManagement.'):
                continue

            seen.add(name)
            problems.append(('unknown permission', os.path.relpath(path, ROOT), name))

    return problems


def main():
    declared, loaded = routes()

    problems = (
        check_links(declared)
        + check_handlers()
        + check_components(loaded)
        + check_permissions()
    )

    print('routes declared      : %d' % len(declared))
    print('lazy loads           : %d' % len(loaded))
    print('problems             : %d' % len(problems))

    for kind, where, what in sorted(problems):
        print('  %-18s %-52s %s' % (kind, where, what))

    if not problems:
        print('\nevery link, every nav entry, every click handler and every guard resolves.')

    # Says what it cannot see, so nobody reads a clean run as more than it is.
    print('\nNot checked here: whether a handler that exists does anything worth')
    print('doing, whether a screen is reachable from any other screen, or whether')
    print('a server route has a client at all. Those need the running app —')
    print('tools/smoke-routes.js and the live Playwright project cover part of it.')

    return 1 if problems else 0


if __name__ == '__main__':
    sys.exit(main())
