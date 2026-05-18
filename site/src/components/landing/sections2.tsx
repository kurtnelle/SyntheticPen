import { useEffect, useRef, useState } from 'react';
import { useReveal, SectionHeader } from './sections';
import { LogoMark } from './icons';
import { DOWNLOAD_URL, RELEASES, REPO, ISSUES, TEXTTOSVG, DOCS } from '../../lib/links';

function UseCaseDemo({ id }: { id: string }) {
  const [t, setT] = useState(0);
  useEffect(() => {
    let raf = 0;
    const start = performance.now();
    const loop = (now: number) => { setT(((now - start) / 4200) % 1); raf = requestAnimationFrame(loop); };
    raf = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(raf);
  }, [id]);

  const drawings: Record<string, string> = {
    sig: 'M 40 130 C 60 80 100 60 130 90 C 150 110 145 140 130 150 C 115 158 105 145 115 130 C 135 110 175 110 195 130 C 210 145 210 165 195 165 C 180 165 180 145 200 145 C 230 145 250 165 270 165 C 300 165 305 110 330 110 C 350 110 358 145 340 160',
    pres: 'M 40 80 L 360 80 M 40 110 L 280 110 M 40 140 L 320 140 M 40 170 L 220 170',
    a11y: 'M 50 130 L 80 80 L 110 130 L 80 105 M 130 130 L 130 80 M 130 130 L 170 130 L 170 80 M 195 90 C 195 80 230 80 230 90 L 230 130 M 195 130 L 230 130',
    svg: 'M 60 100 C 60 70 100 60 130 90 S 200 130 230 100 S 280 70 320 90 S 360 130 340 160',
    auto: 'M 50 60 L 50 180 M 50 60 L 350 60 M 50 120 L 350 120 M 50 180 L 350 180 M 130 60 L 130 180 M 250 60 L 250 180',
  };
  const d = drawings[id] || drawings.sig;
  const pathRef = useRef<SVGPathElement>(null);
  const [pt, setPt] = useState({ x: 50, y: 130 });
  const [len, setLen] = useState(0);
  useEffect(() => { if (pathRef.current) setLen(pathRef.current.getTotalLength()); }, [id, d]);
  useEffect(() => {
    if (pathRef.current && len) {
      const p = pathRef.current.getPointAtLength(t * len);
      setPt({ x: p.x, y: p.y });
    }
  }, [t, len]);

  return (
    <svg viewBox="0 0 400 240" width="100%" height="100%" style={{ display: 'block', background: 'rgba(10,10,10,0.6)' }}>
      <defs>
        <pattern id="ucGrid" x="0" y="0" width="20" height="20" patternUnits="userSpaceOnUse">
          <path d="M 20 0 L 0 0 0 20" fill="none" stroke="rgba(77, 163, 255, 0.07)" strokeWidth="0.5" />
        </pattern>
        <linearGradient id="ucInk" x1="0%" y1="0%" x2="100%" y2="0%">
          <stop offset="0%" stopColor="#4DA3FF" />
          <stop offset="100%" stopColor="#6BE6FF" />
        </linearGradient>
      </defs>
      <rect x="0" y="0" width="400" height="240" fill="url(#ucGrid)" />
      <text x="14" y="22" fontFamily="JetBrains Mono, monospace" fontSize="9" fill="rgba(166,166,166,0.4)" letterSpacing="1">target_app.exe</text>
      <text x="14" y="228" fontFamily="JetBrains Mono, monospace" fontSize="9" fill="rgba(166,166,166,0.4)" letterSpacing="1">replay · {(t * 100).toFixed(0)}%</text>
      <path ref={pathRef} d={d} fill="none" stroke="rgba(255,255,255,0.1)" strokeWidth="1" strokeDasharray="2 3" />
      <path d={d} fill="none" stroke="url(#ucInk)" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" pathLength="1" strokeDasharray="1" strokeDashoffset={1 - t} style={{ filter: 'drop-shadow(0 0 6px rgba(77,163,255,0.7))', opacity: 0.65 }} />
      <path d={d} fill="none" stroke="#E8F4FF" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round" pathLength="1" strokeDasharray="1" strokeDashoffset={1 - t} />
      <circle cx={pt.x} cy={pt.y} r="4" fill="#6BE6FF" style={{ filter: 'drop-shadow(0 0 10px #6BE6FF)' }} />
      <g transform={`translate(${pt.x}, ${pt.y})`}>
        <path d="M 0 0 L 0 14 L 4 11 L 6 16 L 8.5 15 L 6.5 10 L 12 10 Z" fill="#F5F5F5" stroke="#0A0A0A" strokeWidth="0.5" strokeLinejoin="round" />
      </g>
    </svg>
  );
}

export function UseCases() {
  const cases = [
    { id: 'sig', label: 'Signatures', desc: 'Replay a stored signature into any form, PDF, or signing surface that accepts pen input.' },
    { id: 'pres', label: 'Presentations', desc: 'Annotate slides with pre-authored ink. Pre-scripted handwriting on Whiteboard, OneNote, Concepts.' },
    { id: 'a11y', label: 'Accessibility', desc: 'Plot vector glyphs as handwriting for users who cannot hold a stylus. Voice → vector → motion.' },
    { id: 'svg', label: 'SVG Replay', desc: 'Drop an .svg onto SyntheticPen and watch the pointer trace it across any canvas surface.' },
    { id: 'auto', label: 'Annotation Automation', desc: 'Drive QA tooling and design reviews. Repeatable marks for screen capture, demos, and regression.' },
  ];
  const [active, setActive] = useState(0);
  const c = cases[active];
  const ref = useReveal();
  return (
    <section id="use" className="container" style={{ paddingTop: 80, paddingBottom: 80 }}>
      <SectionHeader num="03 / USE CASES" eyebrow="Applications" title="Anywhere the OS accepts a pointer." body="SyntheticPen produces native input. Any Windows application that listens for mouse, pen, or stylus events will receive its motion — no plugins, no shims." />
      <div ref={ref} className="reveal" style={{ display: 'grid', gridTemplateColumns: 'minmax(280px, 380px) 1fr', gap: 24, alignItems: 'stretch' }}>
        <div className="panel" style={{ padding: 0, overflow: 'hidden' }}>
          {cases.map((cs, i) => (
            <button key={cs.id} onClick={() => setActive(i)} style={{ width: '100%', textAlign: 'left', padding: '20px 22px', background: i === active ? 'rgba(77, 163, 255, 0.06)' : 'transparent', border: 'none', borderBottom: i < cases.length - 1 ? '1px solid var(--border)' : 'none', borderLeft: i === active ? '2px solid var(--blue)' : '2px solid transparent', color: 'var(--ink)', cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 16, fontFamily: 'Inter, sans-serif', transition: 'background 0.18s, border-color 0.18s' }}>
              <div>
                <div className="mono" style={{ fontSize: 10, letterSpacing: '0.2em', color: i === active ? 'var(--blue)' : 'var(--ink-dim)' }}>0{i + 1}</div>
                <div style={{ fontSize: 17, fontWeight: 500, marginTop: 4, fontFamily: 'Space Grotesk, sans-serif', color: 'var(--ink)' }}>{cs.label}</div>
              </div>
              <svg width="14" height="14" viewBox="0 0 14 14" fill="none" style={{ opacity: i === active ? 1 : 0.3, transition: 'opacity 0.2s' }}>
                <path d="M3 7H11M11 7L7 3M11 7L7 11" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
              </svg>
            </button>
          ))}
        </div>
        <div className="panel" style={{ padding: 0, overflow: 'hidden', minHeight: 460, display: 'flex', flexDirection: 'column' }}>
          <div style={{ padding: '14px 20px', borderBottom: '1px solid var(--border)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span className="mono" style={{ fontSize: 10, color: 'var(--ink-dim)', letterSpacing: '0.2em' }}>PREVIEW · {c.label.toUpperCase()}</span>
            <div style={{ display: 'flex', gap: 6 }}>
              <span style={{ width: 8, height: 8, borderRadius: '50%', background: 'rgba(255,255,255,0.15)' }} />
              <span style={{ width: 8, height: 8, borderRadius: '50%', background: 'rgba(255,255,255,0.15)' }} />
              <span style={{ width: 8, height: 8, borderRadius: '50%', background: 'var(--blue)', boxShadow: '0 0 6px var(--blue)' }} />
            </div>
          </div>
          <div style={{ flex: 1, position: 'relative' }}><UseCaseDemo id={c.id} /></div>
          <div style={{ padding: '18px 22px', borderTop: '1px solid var(--border)', background: 'rgba(10,10,10,0.5)' }}>
            <p style={{ margin: 0, fontSize: 14, color: 'var(--silver)', lineHeight: 1.55 }}>{c.desc}</p>
          </div>
        </div>
      </div>
    </section>
  );
}

function MotionProfile() {
  const [t, setT] = useState(0);
  useEffect(() => {
    let raf = 0;
    const start = performance.now();
    const loop = (now: number) => { setT(((now - start) / 4000) % 1); raf = requestAnimationFrame(loop); };
    raf = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(raf);
  }, []);
  const profile = (x: number) => {
    if (x < 0.25) return 0.5 * (1 - Math.cos(Math.PI * (x / 0.25)));
    if (x < 0.75) return 1;
    return 0.5 * (1 + Math.cos(Math.PI * ((x - 0.75) / 0.25)));
  };
  const W = 360, H = 240;
  const pad = { l: 36, r: 16, t: 24, b: 30 };
  const pw = W - pad.l - pad.r;
  const ph = H - pad.t - pad.b;
  const N = 80;
  const points = Array.from({ length: N + 1 }, (_, i) => {
    const x = i / N;
    return [pad.l + x * pw, pad.t + (1 - profile(x)) * ph] as [number, number];
  });
  const linePath = 'M ' + points.map((p) => p.join(' ')).join(' L ');
  const head = points[Math.floor(t * N)];
  return (
    <div className="panel" style={{ padding: 0, overflow: 'hidden' }}>
      <div style={{ padding: '14px 20px', borderBottom: '1px solid var(--border)', display: 'flex', justifyContent: 'space-between' }}>
        <span className="mono" style={{ fontSize: 10, color: 'var(--ink-dim)', letterSpacing: '0.2em' }}>VELOCITY PROFILE · S-CURVE</span>
        <span className="mono" style={{ fontSize: 10, color: 'var(--blue)', letterSpacing: '0.2em' }}>jerk-limited</span>
      </div>
      <svg viewBox={`0 0 ${W} ${H}`} width="100%" height="auto" style={{ display: 'block' }}>
        <g stroke="rgba(255,255,255,0.05)" strokeWidth="0.5">
          {[0, 1, 2, 3, 4].map((i) => <line key={'h' + i} x1={pad.l} y1={pad.t + (i / 4) * ph} x2={W - pad.r} y2={pad.t + (i / 4) * ph} />)}
          {[0, 1, 2, 3, 4, 5, 6, 7, 8].map((i) => <line key={'v' + i} x1={pad.l + (i / 8) * pw} y1={pad.t} x2={pad.l + (i / 8) * pw} y2={pad.t + ph} />)}
        </g>
        <line x1={pad.l} y1={pad.t + ph} x2={W - pad.r} y2={pad.t + ph} stroke="rgba(166,166,166,0.3)" />
        <line x1={pad.l} y1={pad.t} x2={pad.l} y2={pad.t + ph} stroke="rgba(166,166,166,0.3)" />
        <text x={pad.l - 8} y={pad.t + 6} fontFamily="JetBrains Mono, monospace" fontSize="9" fill="rgba(166,166,166,0.6)" textAnchor="end">v</text>
        <text x={W - pad.r} y={pad.t + ph + 16} fontFamily="JetBrains Mono, monospace" fontSize="9" fill="rgba(166,166,166,0.6)" textAnchor="end">t</text>
        <text x={pad.l - 8} y={pad.t + ph} fontFamily="JetBrains Mono, monospace" fontSize="8" fill="rgba(166,166,166,0.4)" textAnchor="end">0</text>
        <text x={pad.l - 8} y={pad.t + 4} fontFamily="JetBrains Mono, monospace" fontSize="8" fill="rgba(166,166,166,0.4)" textAnchor="end">vmax</text>
        <rect x={pad.l} y={pad.t} width={pw * 0.25} height={ph} fill="rgba(77,163,255,0.05)" />
        <rect x={pad.l + pw * 0.75} y={pad.t} width={pw * 0.25} height={ph} fill="rgba(77,163,255,0.05)" />
        <path d={linePath} fill="none" stroke="rgba(77,163,255,0.35)" strokeWidth="3" strokeLinecap="round" style={{ filter: 'drop-shadow(0 0 8px rgba(77,163,255,0.55))' }} />
        <path d={linePath} fill="none" stroke="#6BE6FF" strokeWidth="1.4" strokeLinecap="round" />
        {head && (
          <>
            <line x1={head[0]} y1={pad.t} x2={head[0]} y2={pad.t + ph} stroke="rgba(107,230,255,0.4)" strokeWidth="0.5" strokeDasharray="2 3" />
            <circle cx={head[0]} cy={head[1]} r="3.5" fill="#6BE6FF" style={{ filter: 'drop-shadow(0 0 6px #6BE6FF)' }} />
            <text x={head[0] + 8} y={head[1] - 8} fontFamily="JetBrains Mono, monospace" fontSize="9" fill="#6BE6FF">v={profile(t).toFixed(2)}</text>
          </>
        )}
        <text x={pad.l + pw * 0.125} y={pad.t + ph + 16} fontFamily="JetBrains Mono, monospace" fontSize="8" fill="rgba(166,166,166,0.5)" textAnchor="middle">accel</text>
        <text x={pad.l + pw * 0.5} y={pad.t + ph + 16} fontFamily="JetBrains Mono, monospace" fontSize="8" fill="rgba(166,166,166,0.5)" textAnchor="middle">cruise</text>
        <text x={pad.l + pw * 0.875} y={pad.t + ph + 16} fontFamily="JetBrains Mono, monospace" fontSize="8" fill="rgba(166,166,166,0.5)" textAnchor="middle">decel</text>
      </svg>
    </div>
  );
}

export function Technology() {
  const ref = useReveal();
  const rows: [string, string][] = [
    ['Input', 'SVG 1.1 paths · TTF/OTF glyph outlines'],
    ['Centerline', 'Euclidean distance transform + skeletonization'],
    ['Resampler', 'Adaptive arc-length · centripetal Catmull–Rom'],
    ['Velocity', 'Curvature-aware pacing (2⁄3 power law)'],
    ['Pressure', 'Derived from stroke radius'],
    ['Dispatcher', 'Synthetic pen injection · SendInput fallback'],
    ['Platform', 'Windows 10/11 · x64 + Arm64 · MSIX'],
    ['Telemetry', 'None · no network, no analytics'],
  ];
  return (
    <section id="tech" className="container" style={{ paddingTop: 80, paddingBottom: 80 }}>
      <SectionHeader num="04 / TECHNOLOGY" eyebrow="Under the hood" title="Built like motion control hardware." body="A curvature-aware velocity model and a native Win32 pen-injection path. SyntheticPen moves your cursor with the discipline a CNC controller brings to a tool head." />
      <div ref={ref} className="reveal" style={{ display: 'grid', gridTemplateColumns: '1.1fr 1fr', gap: 24, alignItems: 'start' }}>
        <div className="panel" style={{ padding: 0, overflow: 'hidden' }}>
          <div style={{ padding: '14px 20px', borderBottom: '1px solid var(--border)', display: 'flex', justifyContent: 'space-between' }}>
            <span className="mono" style={{ fontSize: 10, color: 'var(--ink-dim)', letterSpacing: '0.2em' }}>SPECIFICATIONS</span>
            <span className="mono" style={{ fontSize: 10, color: 'var(--blue)', letterSpacing: '0.2em' }}>free</span>
          </div>
          {rows.map(([k, v], i) => (
            <div key={i} style={{ display: 'grid', gridTemplateColumns: '130px 1fr', padding: '14px 20px', borderBottom: i < rows.length - 1 ? '1px solid var(--border)' : 'none', gap: 16, alignItems: 'center' }}>
              <span className="mono" style={{ fontSize: 11, color: 'var(--ink-dim)', letterSpacing: '0.12em', textTransform: 'uppercase' }}>{k}</span>
              <span style={{ fontSize: 13, color: 'var(--ink)', fontFamily: 'JetBrains Mono, monospace' }}>{v}</span>
            </div>
          ))}
        </div>
        <MotionProfile />
      </div>
    </section>
  );
}

export function CTA() {
  const ref = useReveal();
  return (
    <section id="cta" style={{ paddingTop: 100, paddingBottom: 100, position: 'relative' }}>
      <div className="container">
        <div ref={ref} className="reveal panel" style={{ padding: '64px 56px', textAlign: 'center', position: 'relative', overflow: 'hidden', borderColor: 'rgba(77, 163, 255, 0.2)' }}>
          <div style={{ position: 'absolute', inset: 0, background: 'radial-gradient(ellipse 60% 80% at 50% 100%, rgba(77,163,255,0.12), transparent 60%)', pointerEvents: 'none' }} />
          <div style={{ position: 'relative' }}>
            <span className="tag" style={{ marginBottom: 28 }}><span className="dot" />Windows 10/11 (x64)</span>
            <h2 className="heading" style={{ fontSize: 'clamp(36px, 5vw, 64px)', fontWeight: 700, lineHeight: 1.05, letterSpacing: '-0.02em', margin: '24px auto 0', maxWidth: 760, textTransform: 'uppercase' }}>
              Bring your geometry into motion.
            </h2>
            <p className="subhead" style={{ maxWidth: 520, margin: '20px auto 0' }}>Free on the Microsoft Store. No telemetry. Single signed binary.</p>
            <div style={{ display: 'flex', justifyContent: 'center', gap: 16, marginTop: 36, flexWrap: 'wrap' }}>
              <a className="btn-primary" href={DOWNLOAD_URL} target="_blank" rel="noopener">
                Download — Free
              </a>
              <a className="btn-ghost" href={DOCS}>Read the docs</a>
            </div>
            <div style={{ marginTop: 16, fontSize: 13, color: 'var(--ink-dim)' }}>
              Can't use the Store?{' '}
              <a href={RELEASES} target="_blank" rel="noopener" style={{ color: 'var(--silver)', borderBottom: '1px solid rgba(166,166,166,0.4)' }}>
                Download the MSI installer
              </a>
            </div>
            <div className="mono" style={{ fontSize: 11, letterSpacing: '0.15em', color: 'var(--ink-dim)', marginTop: 28, textTransform: 'uppercase' }}>
              MICROSOFT STORE · WINDOWS 10/11 · x64 + ARM64
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

export function Footer() {
  return (
    <footer style={{ borderTop: '1px solid var(--border)', padding: '40px 0 32px', position: 'relative', zIndex: 2 }}>
      <div className="container" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 20 }}>
        <div className="logo">
          <LogoMark className="logo-mark" />
          <span>Synthetic<span style={{ color: 'var(--ink-dim)', fontWeight: 400 }}>Pen</span></span>
        </div>
        <div className="mono" style={{ fontSize: 11, letterSpacing: '0.15em', color: 'var(--ink-dim)' }}>© 2026 · BUILT FOR PRECISION</div>
        <div style={{ display: 'flex', gap: 24 }}>
          <a className="nav-link" style={{ fontSize: 11 }} href={DOCS}>Docs</a>
          <a className="nav-link" style={{ fontSize: 11 }} href={RELEASES} target="_blank" rel="noopener">Releases</a>
          <a className="nav-link" style={{ fontSize: 11 }} href={TEXTTOSVG} target="_blank" rel="noopener">Text → SVG</a>
          <a className="nav-link" style={{ fontSize: 11 }} href={REPO} target="_blank" rel="noopener">GitHub</a>
          <a className="nav-link" style={{ fontSize: 11 }} href={ISSUES} target="_blank" rel="noopener">Contact</a>
        </div>
      </div>
    </footer>
  );
}
