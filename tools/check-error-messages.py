# Every screen that reports a failure, against the one reader that knows how.
#
#   python tools/check-error-messages.py
#
# `HttpErrorResponse.message` is written for a developer reading a console:
#
#   Http failure response for https://localhost:44373/api/assessment/exams: 401 Unauthorized
#
# Twenty-four screens were showing it to whoever was trying to get their work
# done. They were swept into two shared readers — `failureReason` for staff,
# `takerFailure` for a candidate, who has no account and must not be told to
# sign in — and the sweep was reported as complete.
#
# It was not. Four places survived it, each for a different reason, and that is
# the whole argument for this file:
#
#   * two screens kept a private `reason()` that still ended at `.message`,
#     so replacing the readers walked straight past them;
#   * `exam-list` had it inline, twice, with no helper to replace;
#   * `media-field` is in `shared/`, and the sweep searched `features/`.
#
# A grep for `e.message` finds none of them, because the code says `e?.message`.
# One pattern is not a search, and a sweep with no guard is swept again.

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
APP = os.path.join(ROOT, 'angular', 'src', 'app')

# The fallback itself, in any spelling: `?.message`, `.message`, with `??` or
# `||` after it. What makes it a defect is that it is a fallback — the value
# reached when the server said nothing a person can read.
RAW_FALLBACK = re.compile(r'\??\.\s*message\s*(?:\?\?|\|\|)')

# Where the decision is allowed to live.
READERS = (
    os.path.join('core', 'failure.ts'),
    os.path.join('take', 'taker-failure.ts'),
)


def is_reader(path):
    return any(path.endswith(reader) for reader in READERS)


def main():
    offenders = []
    scanned = 0

    for base, dirs, files in os.walk(APP):
        dirs[:] = [d for d in dirs if d != 'node_modules']

        for name in files:
            if not name.endswith('.ts') or name.endswith('.spec.ts'):
                continue

            path = os.path.join(base, name)

            if is_reader(path):
                continue

            scanned += 1
            text = io.open(path, encoding='utf-8-sig', errors='ignore').read()

            for number, line in enumerate(text.split('\n'), start=1):
                if line.lstrip().startswith(('*', '//')):
                    continue

                if RAW_FALLBACK.search(line):
                    offenders.append((os.path.relpath(path, ROOT), number))

    print('client files scanned      : %d' % scanned)
    print('screens falling back to a raw message : %d' % len(offenders))

    if offenders:
        print('')
        for path, number in offenders:
            print('  %s:%d' % (path, number))
        print('')
        print('  Use failureReason(err, this.t), or takerFailure for a candidate,')
        print('  who has no account and must never be told to sign in.')
        return 1

    return 0


if __name__ == '__main__':
    sys.exit(main())
