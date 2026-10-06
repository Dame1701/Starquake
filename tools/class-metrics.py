#!/usr/bin/env python3
"""
How much state each class owns - the measurement GODCLASSES.md argues by.

That document's case is that length is the wrong thing to measure: "a class with forty short,
well-named methods over thirty-six fields with no owner is harder to reason about than a class
with one long method". So it counts mutable fields instead, and the four columns here are its
four: total lines, code lines (less blanks and comments, because several of these files are a
fifth `///` and the raw count flatters them), readonly backing fields as "held", and everything
else that is not a constant as "mutable".

DECOUPLING.md refers to "the metric script" twice, including once to record that it had a bug -
its identifier list was missing plain `gameEngine`, which under-reported four files as using no
members at all. The script itself was never in the repository, so neither the numbers nor the bug
could be checked by anyone reading about them. This is that script, or near enough to it: its
definitions are calibrated against the figures those documents already quote, which is the only
test available for it. NastyBrain at 941 lines, 576 of code and 6 mutable fields comes back as
943 / 575 / 6, and ScreenCanvas's 1 held field comes back as 1.

What it cannot do is tell a god class from a value object. Eleven mutable fields is the whole
point of `ColourFaderAnimation`, which is the state of one pulsing tile, and seven is the whole
point of `PlayerKeySet`. Read the mutable count against the code count, the way the table in
GODCLASSES.md does by only listing large classes - and read both against what the class is for.

It is also blind to the job `GameScreenCollection` was doing. That class was four jobs in one
file and never appeared in the table, because it held two mutable fields the whole time. Owning
too much state is one way to be hard to change and this finds it; being the answer to four
unrelated questions is another, and nothing here will ever see it.
"""

import glob
import os
import re
import sys

FIELD = re.compile(
    r'^\s*(?:\[[^\]]*\]\s*)?'
    r'((?:(?:public|private|internal|protected|static|readonly|const|volatile|required)\s+)*)'
    r'([\w<>,\[\]\?\.\s]+?)\s+(_?\w+)\s*(?:=.*)?;\s*$')

TYPE_DECL = re.compile(r'^(internal|public|abstract|sealed|static|partial|file)\b.*\b(class|struct|record)\b')


def measure(path):
    """Total lines, code lines, held (readonly) fields, mutable fields."""
    lines = open(path, encoding='utf-8-sig').read().split('\n')

    code = sum(1 for line in lines
               if line.strip() and not line.strip().startswith(('//', '///', '/*', '*')))

    held = mutable = 0
    depth = 0
    in_type = False

    for line in lines:
        stripped = line.strip()

        if TYPE_DECL.match(stripped):
            in_type = True

        # The depth the line opens at, so a field's own line is read at depth 1 rather than at
        # whatever its initialiser's braces leave behind.
        opened_at = depth
        depth += line.count('{') - line.count('}')

        if not in_type or opened_at != 1 or stripped.startswith(('//', '///')):
            continue

        match = FIELD.match(line)
        if not match:
            continue

        modifiers, declared_type, _ = match.groups()

        # Constants and statics are excluded by the definition; a property or a method can match
        # the shape of a field declaration and is not one.
        if 'const' in modifiers or 'static' in modifiers:
            continue
        if '(' in declared_type or '=>' in line or '{ get' in line:
            continue

        if 'readonly' in modifiers:
            held += 1
        else:
            mutable += 1

    return len(lines), code, held, mutable


def main():
    roots = sys.argv[1:] or ['Starquake.Shared', 'ScreenEditor', 'SharedLibs']
    rows = []

    for root in roots:
        for path in glob.glob(f'{root}/**/*.cs', recursive=True):
            if '/obj/' in path or '/bin/' in path:
                continue
            total, code, held, mutable = measure(path)
            rows.append((mutable, code, total, held, os.path.basename(path)[:-3]))

    rows.sort(reverse=True)

    print(f"{'class':30}{'lines':>7}{'code':>7}{'held':>6}{'mutable':>9}")
    for mutable, code, total, held, name in rows:
        if mutable < 5 and code < 300:
            continue
        print(f'{name:30}{total:7}{code:7}{held:6}{mutable:9}')

    return 0


if __name__ == '__main__':
    sys.exit(main())
