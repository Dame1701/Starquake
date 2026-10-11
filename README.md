# Starquake

A remake of *Starquake* — Stephen Crow's 1985 platform game for the ZX Spectrum, published by
Bubble Bus — rebuilt in C# on MonoGame for macOS, Windows and Linux.

> From the great abyss of a black hole emerges a danger of awesome consequences. To the rescue:
> BLOB, hero of the moment.

## The game

An unstable planet has emerged from a black hole at the edge of the galaxy. If its core is not
rebuilt the planet implodes and takes the universe with it. You are BLOB — Bio-Logically Operated
Being — chosen not for courage or intellect but because everyone else had pranged their spaceship.

The planet is 512 flick-screens, sixteen across and thirty-two down. You crash-land on it, make
your way to the core, and bring it the nine pieces it is missing. Finding them is the game:

- **Bridges** build a platform under you to reach what you cannot jump to, then crumble away.
- **Flying platforms** are faster than walking, but you cannot carry or place anything while riding one.
- **Teleports** are the short cut home, once you know the codes.
- **The access card** opens the security doors and the trading pyramids, which swap an object you
  are carrying for one you would rather have.
- **The key** opens the locked doors, and it is somewhere different every game.
- **Nasties** drain your energy, except the spinning tops, which simply kill you. The laser deals
  with them while its energy lasts.

Energy, laser charge and bridges are all expendable and all replenishable if you look.

## Playing

Movement is digital: cursor keys by default, with **Left Alt** to fire. A connected gamepad works
too — d-pad or left stick, **A** to fire.

The options screen offers four schemes: joypad only, cursor keys with Left Alt, the original
Spectrum layout (**Q A O P** and **Space**), or define all seven keys yourself. Pause and quit come
with the scheme — **P** and **Q** on the modern ones, **S** and **D** on the Spectrum one.

These work everywhere, in both debug and release builds:

| Key | |
| --- | --- |
| `F5` | Save a screenshot |
| `F8` | Swap between the redrawn artwork and the original 1985 graphics |
| `F9` | Step the CRT filter: off → scanlines → phosphor grille → bowed tube |
| `F10` | Backdrops on or off |
| `F11` | Switch between 50Hz and 60Hz |
| `F12` | Fullscreen |

## Building

```sh
cd src
dotnet build Starquake.sln                              # everything
dotnet build Starquake.Mac/Starquake.Mac.csproj         # macOS
dotnet build Starquake.Linux/Starquake.Linux.csproj     # Linux
dotnet build Starquake.Windows/Starquake.Windows.csproj # Windows
dotnet test Starquake.Tests/Starquake.Tests.csproj
```

Needs the .NET 10 SDK, and 8.0 alongside it — MGCB, the content builder, is a net8.0 application.
Every project compiles on any of the three platforms; the macOS build also wraps its output in an
ad-hoc signed `Starquake.app`. A NativeAOT publish profile per platform ships the game as one
native executable with no runtime to install.

`src/plans/BUILD.md` is the full account: project layout, why two of the projects multi-target, what
a publish actually produces and what it weighs.

## Layout

| | |
| --- | --- |
| `src/Starquake.Shared` | The game — code, `Content/` and the map in `Data/` |
| `src/Starquake.Mac`, `.Linux`, `.Windows` | Thin platform heads: target framework, MonoGame flavour, packaging |
| `src/SharedLibs` | Screen data structures and the constants describing the world, shared with the editor |
| `src/ScreenEditor` | Avalonia map editor. A tool, not part of the game |
| `src/ProjectMercury` | Particle engine, plus its content pipeline extension |
| `src/Starquake.Tests` | 804 tests |
| `docs/` | The website, served by GitHub Pages |

## Checks

`TreatWarningsAsErrors` is on, XML doc comments included, so a warning fails the build rather than
scrolling past. CI builds the solution and runs the tests on all three platforms, attempts the
Windows NativeAOT publish that no one has yet confirmed works, and counts namespace cycles in
`Starquake.Shared` — the count is zero and `src/tools/namespace-cycles.py` is what keeps it there.

## Credits

*Starquake* was written by Stephen Crow and published by Bubble Bus in 1985. This remake is an
independent project and is not affiliated with either.
