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
- Download cards (version, package, size, accent colour): edit `downloads` in `scripts/app.dc.js`.
- Download links: search `index.html` for `href="#"` inside the download cards and point them at your GitHub release files.
- Colours, type and spacing: the `:root` tokens at the top of `styles.css`.
- Keep `scripts/support.js` in place — the page needs it.
