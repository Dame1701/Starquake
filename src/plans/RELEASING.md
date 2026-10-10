# Releasing Starquake

## The one number

`<Version>` in `src/Directory.Build.props` is the only place the version is written down.
Bumping it is the release process. There is no tag to push, no second number to keep in step,
and nothing to remember beyond the edit itself.

```xml
<Version>1.0.0</Version>
```

The tag is an *output* of releasing rather than the trigger for it. `gh release create` in the
workflow makes `v<version>` itself, pointing at the commit that was actually built, so the tag is
a record of what shipped rather than an instruction given beforehand.

That ordering is what makes the whole thing idempotent. The `version` job asks git whether
`refs/tags/v<version>` already exists and sets `release=false` if it does, so re-running the
workflow, pushing a documentation fix, or coming back to it a year later publishes nothing as
long as the number has not moved.

## Cutting a new release

1. Edit `<Version>` in `src/Directory.Build.props`.
2. Commit and push to `main`.
3. Wait. CI builds every platform and publishes the release at the end.

```sh
# 1. bump
vi src/Directory.Build.props

# 2. ship
git commit -am "Version 1.0.1"
git push

# 3. watch
gh run watch
```

That is the whole process. The run takes roughly ten minutes, most of it the five NativeAOT
publishes, which cannot be hurried because they each have to compile on their own hardware.

### Pre-releases

A hyphen makes it one, which is semver's own convention and what the `version` job keys off:

| `<Version>` | Result |
| --- | --- |
| `1.0.1` | Release, marked **Latest** |
| `1.0.1-rc1` | Release, flagged pre-release, **not** marked Latest |

A pre-release is a real release with real assets. It can go to people to try without displacing
the version everyone else downloads.

### Dry runs

To exercise the entire pipeline on `main` and publish nothing:

```sh
gh workflow run build.yml --ref main -f dry_run=true
```

**`dry_run` defaults to `true` on a manual run.** This catches people out: dispatching the
workflow from the Actions page and leaving the box ticked builds everything and releases nothing,
with no error to say so. A push to `main` is unaffected - `inputs.dry_run` is empty there, not
`true`, so an ordinary push releases normally.

## Redoing a release that already exists

Sometimes a version needs to ship again from a newer commit rather than moving to a new number.
The tag check means CI will not do it twice, so the old release and its tag have to go first.

```sh
gh release delete v1.0.0 --cleanup-tag --yes
gh workflow run build.yml --ref main -f dry_run=false
```

Two things about that are worth knowing:

- **`--cleanup-tag` is not optional.** It removes the release *and* the tag together. Deleting
  only the tag is worse than doing nothing: the orphaned release keeps the name, the `version`
  job then sees no tag and decides to release, and `gh release create` fails at the very end of a
  ten minute run because a release with that tag already exists.
- **`-f dry_run=false` is not optional either**, for the reason in "Dry runs" above. Pushing any
  commit to `main` works just as well and needs no flag.

**The rebuild comes from current `main`, not from the commit the original release was built at.**
The number stays the same and the binaries do not. If that matters - if the version has already
gone to anyone - bump to the next number instead, which is what the workflow is designed around.
Nothing is lost either way: the original commit is still in history, so the old binaries can be
rebuilt from it if they are ever wanted.

## What ships

Five NativeAOT publishes become eight assets. None of them needs a .NET runtime installed.

| Platform | Asset | |
| --- | --- | --- |
| macOS (Apple Silicon) | `Starquake-<version>-osx-arm64.dmg` | Drag to Applications |
| macOS (Intel) | `Starquake-<version>-osx-x64.dmg` | Drag to Applications |
| Windows | `Starquake-<version>-win-x64-Setup.exe` | Inno Setup installer, no administrator |
| | `Starquake-<version>-win-x64.zip` | Portable folder |
| Linux | `Starquake-<version>-linux-x64.AppImage` | `chmod +x` and run |
| | `Starquake-<version>-linux-x64.tar.gz` | Portable folder |
| Linux (ARM) | `Starquake-<version>-linux-arm64.AppImage` | `chmod +x` and run |
| | `Starquake-<version>-linux-arm64.tar.gz` | Portable folder |

The release notes are written by the workflow, not by hand - they are a heredoc in the `release`
job and the same text every time. Editing them means editing `build.yml`.

## What runs, and in what order

```
version ──┬── content (windows) ──── publish × 5 ──── release
          └── namespaces
             build + test
```

- **`version`** reads `<Version>` and decides whether this run releases at all.
- **`namespaces`** fails in about a second if a namespace cycle has appeared.
- **`build`** compiles and tests with `PrebuiltContentDir` pointed at an empty folder, so it
  reports a compile error in two minutes rather than after MGCB has ground through the artwork.
- **`content`** builds the `.xnb` once, on Windows, because Windows is the only runner with a
  shader compiler already on it. Every platform gets the same DesktopGL output.
- **`publish`** compiles one native executable per target, each on its own hardware. NativeAOT
  does not cross-compile, which is why this is a matrix of runners.
- **`release`** runs only when `version` said so, and only after all five publishes succeed.

See BUILD.md for why the content is built once and handed round, and for the Wine that a local
content build needs on macOS and Linux.

## Prerequisites

Only for the `gh` commands above; an ordinary bump-and-push release needs nothing but git.

```sh
gh auth login          # GitHub.com -> HTTPS -> web browser
gh auth status         # should report scopes including 'repo' and 'workflow'
```

`repo` covers deleting and creating releases, `workflow` covers dispatching the build. The
one-time code the browser asks for is printed by `gh` in the terminal, just above the prompt - it
does not come from GitHub.

## Checking what happened

```sh
gh run list --workflow=build.yml --limit 5   # recent runs
gh run watch                                 # follow the current one
gh release list                              # what exists
gh release view v1.0.1                       # one release and its assets
```

If a run finished green but published nothing, read the `version` job's annotation. It says which
of the four reasons applied: dry run, not `main`, already released, or new.
