# Every string the client asks for, against every string the server defines.
#
#   python tools/check-localization.py
#
# A key the client requests and the resource file does not define does not fail,
# and does not warn. ABP hands back the key itself, so the screen shows
# "::Link:Extend" where a sentence should be — in whichever language the reader
# is using, on whichever screen nobody happened to open before shipping.
#
# The reverse is worth knowing too, though it is not a defect: a key defined and
# never asked for is usually a screen that was renamed, and occasionally a
# screen that was never finished.

import io
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOCALES = os.path.join(
    ROOT, 'src', 'InternshipManagementSystem.Domain.Shared',
    'Localization', 'InternshipManagementSystem')


def defined(lang):
    path = os.path.join(LOCALES, lang + '.json')
    with io.open(path, encoding='utf-8-sig') as handle:
        return set(json.load(handle)['texts'])


# t('::Some:Key') in a template or a component, in either quote style.
CALL = re.compile(r"""t\(\s*['"]::([^'"]+)['"]""")

# t('::Candidate:Status:' + row.status) — the key is finished at runtime, so the
# literal half is a prefix and not a key. Without this the prefix is reported
# missing (it is), and every real key under it is reported as defined-but-unused
# (they are not). Both halves of that are noise, and noise is how a checker stops
# being read.
COMPOSED = re.compile(r"""t\(\s*['"]::([^'"]+)['"]\s*\+""")

# A bare '::Some:Key' literal anywhere else. Several screens hold their keys in
# a table — `{ value: ..., labelKey: '::Question:Difficulty:Easy' }` — and hand
# them to t() somewhere far away. Looking only inside t() calls reported all of
# those as defined-and-unused, which is exactly backwards.
LITERAL = re.compile(r"""['"]::([^'"]+)['"]""")

# `::Link:State:${state}` — the same idea as COMPOSED, written the other way.
# Six link states were reported as text nobody shows, and every one of them is
# on the screen a coordinator reads to see whether a link was sent, opened,
# spent or revoked.
TEMPLATE = re.compile(r"""`::([^`$]*)\$\{""")

# The server localises too, and reads its keys the same two ways: L["Some:Key"]
# and L[$"Prefix:{value}"]. Twenty keys behind the question-import template were
# reported as unused because this half was never looked at — the file that reads
# them is C#, and the checker only walked the client.
SERVER_KEY = re.compile(r"""L\[\s*\$?"([^"{]+)(\{)?""")

# And keys the server holds as constants rather than reading inline:
#
#   public const string TypeColumnKey = "QuestionImport:Column:Type";
#
# Written that way on purpose — the template a person downloads and the parser
# that reads it back name their columns from one place, so the two cannot drift.
# A checker that only sees keys at the moment they are looked up calls all eight
# of them dead.
SERVER_CONST = re.compile(
    '"((?:[A-Za-z][A-Za-z0-9]*:)+[A-Za-z0-9:_-]+)"')

# And keys the server *builds* for the client to look up:
#
#   NameKey = $"::QuestionType:{d.Type}"
#
# The client renders `t(descriptor.nameKey)` and never names the key at all, so
# neither side of this shows a literal — thirteen question-type names were
# reported dead while every one of them is on the authoring screen.
SERVER_BUILT = re.compile(
    r'\$"::([^"{]*)\{')


def used():
    found = {}
    prefixes = set()
    literals = set()
    client = os.path.join(ROOT, 'angular', 'src', 'app')

    for base, dirs, files in os.walk(client):
        dirs[:] = [d for d in dirs if d != 'node_modules']

        for name in files:
            if not name.endswith(('.html', '.ts')):
                continue

            path = os.path.join(base, name)
            with io.open(path, encoding='utf-8') as handle:
                text = handle.read()

            partial = set(COMPOSED.findall(text))
            partial |= set(TEMPLATE.findall(text))

            prefixes |= partial
            literals |= set(LITERAL.findall(text))

            for key in CALL.findall(text):
                if key in partial:
                    continue
                found.setdefault(key, set()).add(os.path.relpath(path, ROOT))

    # The server's own keys. It renders the import template's column headings and
    # its sample rows, and those are as much "shown to somebody" as anything in
    # a component.
    server = os.path.join(ROOT, 'src')

    for base, dirs, files in os.walk(server):
        dirs[:] = [d for d in dirs if d not in ('bin', 'obj', 'Migrations')]

        for name in files:
            if not name.endswith('.cs'):
                continue

            path = os.path.join(base, name)
            with io.open(path, encoding='utf-8-sig') as handle:
                text = handle.read()

            for key, interpolated in SERVER_KEY.findall(text):
                # Only keys shaped like ours. ABP's own resources are looked up
                # through the same `L[...]` and are named in bare PascalCase —
                # `InvalidRedirectUri` — so requiring a colon keeps four of
                # somebody else's strings out of our missing list.
                if ':' not in key:
                    continue

                if interpolated:
                    # L[$"QuestionImport:Sample:{n}:Type"] — a prefix, not a key.
                    prefixes.add(key)
                else:
                    found.setdefault(key, set()).add(os.path.relpath(path, ROOT))

            for key in SERVER_CONST.findall(text):
                literals.add(key)

            for prefix in SERVER_BUILT.findall(text):
                prefixes.add(prefix)

    return found, prefixes, literals


def main():
    ar, en = defined('ar'), defined('en')
    asked, prefixes, literals = used()

    def reached(key):
        return (key in asked
                or key in literals
                or any(key.startswith(p) for p in prefixes))

    missing_ar = sorted(k for k in asked if k not in ar)
    missing_en = sorted(k for k in asked if k not in en)

    print('keys the client asks for : %d' % len(asked))
    print('missing from ar.json     : %d' % len(missing_ar))
    print('missing from en.json     : %d' % len(missing_en))

    for lang, missing in (('ar', missing_ar), ('en', missing_en)):
        for key in missing:
            where = sorted(asked[key])[0]
            print('  %s  %-46s %s' % (lang, key, where))

    # Not counted as a failure, and filtered down to the ones a person could
    # act on. A key can be defined ahead of the screen that will use it; and
    # three families are resolved by the server and never pass through t() at
    # all — error codes, and ABP's DisplayName:/Description: convention for
    # naming a setting or a permission in its own definition.
    server_side = ('IMS:', 'DisplayName:', 'Description:', 'Permission:')

    orphans = sorted(
        k for k in ar
        if not reached(k) and not k.startswith(server_side) and ':' in k)

    print('\ndefined in ar.json, asked for nowhere: %d' % len(orphans))
    for key in orphans[:20]:
        print('  %s' % key)
    if len(orphans) > 20:
        print('  ... and %d more' % (len(orphans) - 20))

    # Only a missing key is a failure: that one reaches a reader as raw text.
    return 1 if (missing_ar or missing_en) else 0


if __name__ == '__main__':
    sys.exit(main())
