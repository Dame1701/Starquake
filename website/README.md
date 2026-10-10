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
1. Copy everything in this folder (including `.nojekyll`) into your repo root, or into a `/docs` folder.
2. Repo → Settings → Pages → Deploy from branch → pick the branch and folder.

## Updating content
- Screenshots: replace `assets/screenshot-1.png` … `screenshot-6.png` (keep the names).
- Screenshot titles: edit `shotLabels` in `scripts/app.dc.js`.
- Download cards (version, package, size, accent colour): edit `downloads` in `scripts/app.dc.js`.
- Download links: search `index.html` for `href="#"` inside the download cards and point them at your GitHub release files.
- Colours, type and spacing: the `:root` tokens at the top of `styles.css`.
- Keep `scripts/support.js` in place — the page needs it.
