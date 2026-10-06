#!/usr/bin/env python3
"""
Which namespaces in Starquake.Shared depend on each other both ways.

C# has no rule against this - everything in the game library compiles into one assembly, and
inside one assembly any type may reference any other whatever namespace it sits in. That is
exactly why it drifts: nothing tells you. DECOUPLING.md cut every cycle in the *object* graph,
which is a different axis - it is about what can be constructed before what, and it was fixed by
turning fields into parameters. A `using` does not care whether a dependency is a field or a
parameter, so the namespace graph was never touched by that work and had drifted to fourteen
two-cycles over twenty-two namespaces by the time it was first measured.

It matters for three reasons. A one-way graph can be read in dependency order and a cyclic one
cannot. A one-way dependency is a rule a reviewer can check, where a cycle is a rule already
broken and the next `using` across it costs nothing. And assemblies *do* enforce acyclicity -
C# will not let two projects reference each other - so every cycle here is a compile error on
the day the game logic is split out from the drawing to get Blob and Nasty under test.

The edge source is the `using Starquake.*` lines, which are exact: the codebase fully-qualifies
no type, so a reference across namespaces always needs one. Each edge is reported with the type
that causes it, because that is what turns a cycle into a five-minute fix rather than a mystery -
twelve of the original fourteen were one misfiled file each.

Pairs where one namespace encloses the other are not counted. C# name lookup walks enclosing
namespaces, so GameComponents.GameScreens reaches ScreenDirectory with no `using` at all; that
is containment rather than a dependency.

Exits 1 if there are more cycles than BASELINE, which is what makes this a gate rather than a
note. The three that remain are design rather than filing and are named below.
"""

import collections
import os
import re
import sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Starquake.Shared')

# MainGameScreen holding a GameEngine, and GameScreenCollection needing Blob, CoreElement and
# ColourAnimationEngine while all three need it back. Both are the classes themselves doing too
# much, not files in the wrong folder - relocating GameScreenCollection was tried and the cycle
# simply followed it. Splitting them is the fix; lower this number when they are split.
BASELINE = 2

DECL = re.compile(
    r'^\s*(?:\[[^\]]*\]\s*)?(?:(?:public|internal|protected|private|abstract|sealed|static|partial'
    r'|readonly|ref|file)\s+)*(?:class|struct|interface|enum|record)\s+([A-Za-z_]\w*)', re.M)
DELEGATE = re.compile(r'^\s*(?:public|internal)\s+delegate\s+.*?\s([A-Za-z_]\w*)\s*\(', re.M)
NAMESPACE = re.compile(r'^namespace ([\w.]+);', re.M)


def sources():
    for folder, _, names in os.walk(ROOT):
        if f'{os.sep}obj' in folder or f'{os.sep}bin' in folder:
            continue
        for name in names:
            if name.endswith('.cs'):
                yield os.path.join(folder, name)


def read():
    files = {}
    for path in sources():
        text = open(path, encoding='utf-8-sig').read()
        declared = NAMESPACE.search(text)
        if declared:
            files[os.path.relpath(path, ROOT)] = (declared.group(1), text)
    return files


def edges(files):
    """Namespace -> namespace -> the (file, type) pairs that put it there."""
    home = {}
    for namespace, text in files.values():
        for name in DECL.findall(text) + DELEGATE.findall(text):
            home.setdefault(name, namespace)

    graph = collections.defaultdict(lambda: collections.defaultdict(set))
    for relative, (namespace, text) in files.items():
        # Comments are stripped so that a <see cref> does not count as a dependency, and the
        # using block so that `using Starquake.Display;` does not look like a use of Display.
        body = re.sub(r'^using .*$', '', text, flags=re.M)
        body = re.sub(r'///.*$', '', body, flags=re.M)
        body = re.sub(r'/\*.*?\*/', '', body, flags=re.S)
        body = re.sub(r'^\s*//.*$', '', body, flags=re.M)
        visible = set(re.findall(r'^using (Starquake[\w.]*);', text, re.M)) | {namespace}

        for name in set(re.findall(r'\b([A-Za-z_]\w*)\b', body)):
            owner = home.get(name)
            if owner in visible and owner != namespace:
                graph[namespace][owner].add((relative, name))
    return graph


def cycles(graph):
    found, seen = [], set()
    for first in list(graph):
        for second in graph[first]:
            nested = first.startswith(second + '.') or second.startswith(first + '.')
            if first in graph.get(second, {}) and (second, first) not in seen and not nested:
                seen.add((first, second))
                found.append((first, second, graph[first][second], graph[second][first]))
    return found


def main():
    found = cycles(edges(read()))
    plain = lambda namespace: namespace.replace('Starquake.', '')

    for first, second, forward, back in sorted(found, key=lambda c: min(len(c[2]), len(c[3]))):
        thin, direction = ((forward, f'{plain(first)} -> {plain(second)}')
                           if len(forward) <= len(back)
                           else (back, f'{plain(second)} -> {plain(first)}'))
        causes = ', '.join(sorted(f'{os.path.basename(f)}:{t}' for f, t in thin))
        print(f'{plain(first)} <-> {plain(second)}')
        print(f'    {len(forward)} file(s) one way, {len(back)} the other. Thinner side {direction}:')
        print(f'    {causes}')

    print(f'\n{len(found)} namespace cycles (baseline {BASELINE})')

    if len(found) > BASELINE:
        print('\nFAIL: a new namespace cycle. The type named above is almost always one file in '
              'the wrong folder - move it rather than adding a using.')
        return 1
    if len(found) < BASELINE:
        print(f'\nBASELINE is stale: lower it to {len(found)}.')
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
