/* ==========================================================================
   Starquake — component logic
   Extracted from the inline <script data-dc-script> island in index.html.
   The runtime reads this from that island's text content; index.html loads it
   synchronously just before boot.

   Everything visual lives in styles.css. Values that genuinely vary per item
   (a card's accent colour) are handed to the markup as CSS custom properties;
   on/off states are handed over as class names, never as inline declarations.
   ========================================================================== */

class Component extends DCLogic {
  state = { active: 0, lightbox: false, os: null };

  canvasRef = React.createRef();

  /* ---------- Content ---------- */

  shotLabels = [
    "The Crash Site",
    "Flight Computer Report",
    "Teleportal Code Entry",
    "The Molecule Fields",
    "Violet Stalactite Caves",
    "The Explorer Who Didn't Make It"
  ];

  /* ---------- Downloads ----------
     One release, eight assets. `asset` is the suffix after the version in each
     published file name; the full name and URL are built from it in renderVals,
     so a new release means bumping `version` and the sizes. */

  version = '1.1.0';

  repo = 'https://github.com/Dame1701/Starquake';

  downloads = [
    {
      name: 'macOS',
      req: 'macOS 12+ · Apple Silicon & Intel',
      accent: '#2b3fe0',
      asset: 'osx-arm64.dmg',
      package: 'Apple Silicon .dmg',
      size: '45 MB',
      alternates: [
        { label: 'Intel .dmg', asset: 'osx-x64.dmg', size: '46 MB' }
      ]
    },
    {
      name: 'Windows',
      req: 'Windows 10 / 11 · 64-bit',
      accent: '#e0901f',
      asset: 'win-x64-Setup.exe',
      package: 'Installer .exe',
      size: '33 MB',
      alternates: [
        { label: 'Portable .zip', asset: 'win-x64.zip', size: '38 MB' }
      ]
    },
    {
      name: 'Linux',
      req: 'x64 & ARM64 · most distros',
      accent: '#28c0b0',
      asset: 'linux-x64.AppImage',
      package: 'x64 .AppImage',
      size: '38 MB',
      alternates: [
        { label: 'x64 .tar.gz',     asset: 'linux-x64.tar.gz',      size: '38 MB' },
        { label: 'ARM64 .AppImage', asset: 'linux-arm64.AppImage',  size: '37 MB' },
        { label: 'ARM64 .tar.gz',   asset: 'linux-arm64.tar.gz',    size: '38 MB' }
      ]
    }
  ];

  /* ---------- Lifecycle ---------- */

  componentDidMount() {
    this.setState({ os: this.detectOS() });

    this.onKey = e => {
      if (e.key === 'Escape') this.setState({ lightbox: false });
      if (e.key === 'ArrowRight') this.step(1);
      if (e.key === 'ArrowLeft') this.step(-1);
    };
    window.addEventListener('keydown', this.onKey);

    this.startStarfield();
  }

  componentWillUnmount() {
    cancelAnimationFrame(this.raf);
    window.removeEventListener('keydown', this.onKey);
    window.removeEventListener('resize', this.onResize);
  }

  detectOS() {
    const ua = navigator.userAgent;
    if (/Mac/i.test(ua)) return 'macOS';
    if (/Win/i.test(ua)) return 'Windows';
    if (/Linux|X11/i.test(ua)) return 'Linux';
    return null;
  }

  /* The release URL of one asset, from the suffix recorded in `downloads`. */
  assetUrl(asset) {
    return `${this.repo}/releases/download/v${this.version}/Starquake-${this.version}-${asset}`;
  }

  step(delta) {
    const n = this.shotLabels.length;
    this.setState(s => ({ active: (s.active + delta + n) % n }));
  }

  /* ---------- Starfield ----------
     Parallax star canvas behind the page: stars drift left at a rate set by
     their depth, and scrolling nudges them vertically. The brightest ones get
     a cross-flare. */

  startStarfield() {
    const canvas = this.canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    const greys = ['#ffffff', '#e2e2e6', '#c4c4ca', '#a2a2aa', '#808088', '#5e5e66'];
    // Weight the palette towards grey so the warm/blue stars stay occasional.
    const palette = [...greys, ...greys, ...greys, '#fff2b0', '#ffe9a0', '#bcd6ff', '#a9c8ff'];

    let w, h, stars;

    this.onResize = () => {
      const dpr = Math.min(devicePixelRatio || 1, 2);
      w = innerWidth;
      h = innerHeight;
      canvas.width = w * dpr;
      canvas.height = h * dpr;
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      stars = Array.from({ length: Math.round(w * h / 3500) }, () => ({
        x: Math.random() * w,
        y: Math.random() * h,
        z: Math.random() ** 2 * 0.9 + 0.1,   // depth: near stars are rarer
        t: Math.random() * 6.28,             // twinkle phase
        c: palette[Math.floor(Math.random() * palette.length)]
      }));
    };

    this.onResize();
    window.addEventListener('resize', this.onResize);

    let lastY = scrollY;
    const tick = () => {
      const speed = this.props.starSpeed ?? 1;
      const dy = scrollY - lastY;
      lastY = scrollY;

      ctx.clearRect(0, 0, w, h);
      for (const s of stars) {
        s.x -= s.z * 0.35 * speed;
        s.y -= dy * s.z * 0.25;
        s.t += 0.03;
        if (s.x < 0) s.x += w;
        if (s.y < 0) s.y += h;
        if (s.y > h) s.y -= h;

        const r = s.z * 1.8;
        const alpha = 0.4 + 0.6 * Math.abs(Math.sin(s.t)) * s.z + 0.2;
        ctx.globalAlpha = Math.min(alpha, 1);
        ctx.fillStyle = s.c;
        if (s.z > 0.8) {
          ctx.fillRect(s.x - r * 2, s.y - 0.5, r * 4, 1);
          ctx.fillRect(s.x - 0.5, s.y - r * 2, 1, r * 4);
        }
        ctx.beginPath();
        ctx.arc(s.x, s.y, r, 0, 6.28);
        ctx.fill();
      }
      ctx.globalAlpha = 1;

      this.raf = requestAnimationFrame(tick);
    };
    tick();
  }

  /* ---------- Render values ---------- */

  renderVals() {
    const { active, lightbox, os } = this.state;

    const shots = this.shotLabels.map((label, i) => ({
      src: `assets/screenshot-${i + 1}.png`,
      label,
      stateClass: i === active ? 'is-active' : '',
      select: () => this.setState({ active: i })
    }));

    const highlightOS = this.props.highlightOS ?? true;
    const platforms = this.downloads.map(p => {
      const recommended = highlightOS && p.name === os;
      return {
        ...p,
        url: this.assetUrl(p.asset),
        alternates: p.alternates.map(a => ({ ...a, url: this.assetUrl(a.asset) })),
        recommended,
        stateClass: recommended ? 'is-recommended' : ''
      };
    });

    const total = String(shots.length).padStart(2, '0');

    return {
      canvasRef: this.canvasRef,

      shots,
      activeShot: shots[active],
      shotCounter: `${String(active + 1).padStart(2, '0')} / ${total}`,

      platforms,
      version: `v${this.version}`,
      repoUrl: this.repo,
      releaseUrl: `${this.repo}/releases/tag/v${this.version}`,
      releasesUrl: `${this.repo}/releases`,

      lightboxOpen: lightbox,
      openLightbox: () => this.setState({ lightbox: true }),
      closeLightbox: () => this.setState({ lightbox: false })
    };
  }
}
