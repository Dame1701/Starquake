# Starquake website

Static site for GitHub Pages.

## Layout

```
index.html        markup only — semantic classes, no inline styles
styles.css        all styling: design tokens → base → components → sections
scripts/
  support.js      the DC runtime (don't edit)
  app.dc.js       component logic: starfield, screenshot gallery, OS detection
assets/           hero art, comic panels, screenshots
```

`index.html` holds no CSS or JS of its own. The component logic is loaded from
`scripts/app.dc.js` into the `<script data-dc-script>` island at the bottom of
the page, which is how the runtime expects to find it.

## Previewing

Serve the folder — don't open `index.html` from disk. The loader at the foot of the page pulls
`scripts/app.dc.js` in over a synchronous `XMLHttpRequest`, and `file://` blocks that as
cross-origin, so the runtime boots with no component logic at all. The page still looks mostly
right, because everything written as plain markup renders: what disappears is whatever the script
builds — the download cards, the screenshot gallery and the starfield, all of them silently.

```sh
cd docs && python3 -m http.server 8000   # then http://localhost:8000/
```

Chrome caches `styles.css` aggressively across reloads; after editing it, shift-reload or the page
will look unstyled.

## Publish
GitHub Pages serves this folder as it stands: Settings → Pages → Deploy from branch → `main`,
folder `/docs`.

The folder is named `docs` rather than `website` because branch deployment offers exactly two
choices of folder, `/` and `/docs`, and no way to name a third. Keep `.nojekyll` beside
`index.html` so the files are served as they are instead of being run through Jekyll.

Every path in the page is relative, so it renders correctly at the project URL
(`https://dame1701.github.io/Starquake-Linux/`) as well as at an account root.

## Updating content
- Screenshots: replace `assets/screenshot-1.png` … `screenshot-6.png` (keep the names).
- Screenshot titles: edit `shotLabels` in `scripts/app.dc.js`.
- Download cards (packages, sizes, accent colour): edit `downloads` in `scripts/app.dc.js`. Each
  entry names one headline build and the rest of that platform's builds as `alternates`; `asset` is
  whatever follows the version in the published file name, and the file names and release URLs are
  built from it. A new release means bumping `version` (and `repo`, if it ever moves) and the sizes —
  the links in the markup need no editing.
- Colours, type and spacing: the `:root` tokens at the top of `styles.css`.
- Keep `scripts/support.js` in place — the page needs it.
