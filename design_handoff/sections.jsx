/* global React */
const { useState: useStateS, useEffect: useEffectS, useRef: useRefS, useMemo: useMemoS } = React;

/* ============================================================
   useReveal — fade in when in viewport
   ============================================================ */
const useReveal = () => {
  const ref = useRefS(null);
  useEffectS(() => {
    if (!ref.current) return;
    const io = new IntersectionObserver(entries => {
      entries.forEach(e => { if (e.isIntersecting) e.target.classList.add('in'); });
    }, { threshold: 0.15 });
    io.observe(ref.current);
    return () => io.disconnect();
  }, []);
  return ref;
};

/* ============================================================
   Section header with eyebrow + number + title
   ============================================================ */
const SectionHeader = ({ num, eyebrow, title, body }) => {
  const ref = useReveal();
  return (
    <div ref={ref} className="reveal" style={{
      display: 'grid',
      gridTemplateColumns: '120px 1fr',
      gap: 32,
      alignItems: 'start',
      marginBottom: 56,
      borderTop: '1px solid var(--border)',
      paddingTop: 32
    }}>
      <div>
        <div className="mono" style={{ fontSize: 11, letterSpacing: '0.2em', color: 'var(--blue)' }}>
          {num}
        </div>
        <div className="mono" style={{ fontSize: 11, letterSpacing: '0.2em', color: 'var(--ink-dim)', marginTop: 6, textTransform: 'uppercase' }}>
          {eyebrow}
        </div>
      </div>
      <div>
        <h2 className="section-title" style={{ maxWidth: 720 }}>{title}</h2>
        {body && <p style={{ marginTop: 18, fontSize: 16, color: 'var(--silver)', maxWidth: 640, lineHeight: 1.6 }}>{body}</p>}
      </div>
    </div>
  );
};

/* ============================================================
   What It Is — split panel with code-style readout
   ============================================================ */
const WhatItIs = () => {
  const ref = useReveal();
  return (
    <section id="what" className="container" style={{ paddingTop: 80, paddingBottom: 80 }}>
      <SectionHeader
        num="01 / WHAT IT IS"
        eyebrow="Definition"
        title="A virtual pen plotter that lives inside Windows."
        body="SyntheticPen reads vector geometry — SVG paths, glyph outlines, hand-drawn signatures — and replays it as synthetic mouse and pen input. The system cursor becomes a plotter head, tracing your geometry with sub-pixel precision into any application that accepts input."
      />
      <div ref={ref} className="reveal" style={{
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
        gap: 1,
        background: 'var(--border)',
        border: '1px solid var(--border)',
      }}>
        {[
          { k: 'INPUT', v: 'SVG · DXF · TTF · CSV', label: 'Vector geometry from any source' },
          { k: 'OUTPUT', v: 'SendInput · WM_POINTER', label: 'Native Win32 cursor / pen events' },
          { k: 'PRECISION', v: '0.1 px @ 1000 Hz', label: 'Sub-pixel interpolation' },
          { k: 'LATENCY', v: '< 2 ms', label: 'From queue to dispatch' }
        ].map((s, i) => (
          <div key={i} style={{ padding: '28px 24px', background: 'var(--bg-1)' }}>
            <div className="mono" style={{ fontSize: 10, letterSpacing: '0.2em', color: 'var(--ink-dim)' }}>{s.k}</div>
            <div className="mono" style={{ fontSize: 18, color: 'var(--blue)', marginTop: 12, fontWeight: 500 }}>{s.v}</div>
            <div style={{ fontSize: 13, color: 'var(--silver)', marginTop: 10, lineHeight: 1.5 }}>{s.label}</div>
          </div>
        ))}
      </div>
    </section>
  );
};

/* ============================================================
   Mini illustration: SVG Path Parsing
   ============================================================ */
const IllParsing = () => {
  const [t, setT] = useStateS(0);
  useEffectS(() => {
    let raf;
    const start = performance.now();
    const loop = (now) => {
      setT(((now - start) / 3000) % 1);
      raf = requestAnimationFrame(loop);
    };
    raf = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(raf);
  }, []);
  const pathD = "M 20 80 C 40 20, 60 110, 90 70 S 130 30, 160 75 S 200 100, 230 50";
  return (
    <svg viewBox="0 0 260 120" width="100%" height="auto" style={{ display: 'block' }}>
      <defs>
        <pattern id="ipGrid" x="0" y="0" width="12" height="12" patternUnits="userSpaceOnUse">
          <path d="M 12 0 L 0 0 0 12" fill="none" stroke="rgba(255,255,255,0.04)" strokeWidth="0.4" />
        </pattern>
      </defs>
      <rect x="0" y="0" width="260" height="120" fill="url(#ipGrid)" />
      {/* "File" card behind */}
      <g transform="translate(8, 12)">
        <rect x="0" y="0" width="120" height="80" fill="rgba(26,26,26,0.7)" stroke="rgba(255,255,255,0.12)" strokeWidth="0.6" />
        <text x="6" y="12" fontFamily="JetBrains Mono, monospace" fontSize="7" fill="rgba(166,166,166,0.7)" letterSpacing="1">SVG</text>
        <text x="6" y="24" fontFamily="JetBrains Mono, monospace" fontSize="6" fill="rgba(166,166,166,0.5)">&lt;path d="M20</text>
        <text x="6" y="34" fontFamily="JetBrains Mono, monospace" fontSize="6" fill="rgba(166,166,166,0.5)">80 C40 20</text>
        <text x="6" y="44" fontFamily="JetBrains Mono, monospace" fontSize="6" fill="rgba(166,166,166,0.5)">60 110..."/&gt;</text>
      </g>
      {/* "Parsed" card foreground */}
      <g transform="translate(40, 28)">
        <rect x="0" y="0" width="210" height="80" fill="rgba(18,18,18,0.95)" stroke="rgba(77,163,255,0.4)" strokeWidth="0.6" />
        <path d={pathD} fill="none" stroke="rgba(255,255,255,0.15)" strokeWidth="1" strokeDasharray="2 3" />
        <path d={pathD} fill="none" stroke="#4DA3FF" strokeWidth="1.4"
          pathLength="1" strokeDasharray="1" strokeDashoffset={1 - t}
          style={{ filter: 'drop-shadow(0 0 4px rgba(77,163,255,0.7))' }} />
        {/* Control points */}
        {[[40, 20], [60, 110], [130, 30], [160, 75], [200, 100], [230, 50]].map(([cx, cy], i) => (
          <circle key={i} cx={cx} cy={cy} r="1.6" fill="#6BE6FF" opacity={t > i * 0.16 ? 1 : 0.2} />
        ))}
        <text x="6" y="74" fontFamily="JetBrains Mono, monospace" fontSize="6" fill="rgba(107,230,255,0.7)" letterSpacing="0.5">
          Path
        </text>
      </g>
    </svg>
  );
};

/* ============================================================
   Mini illustration: Motion Planning (bezier with handles)
   ============================================================ */
const IllPlanning = () => {
  const [t, setT] = useStateS(0);
  useEffectS(() => {
    let raf;
    const start = performance.now();
    const loop = (now) => { setT(((now - start) / 3000) % 1); raf = requestAnimationFrame(loop); };
    raf = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(raf);
  }, []);
  // Bezier through points
  const pts = [[30, 90], [70, 30], [130, 80], [180, 35], [230, 70]];
  const path = `M ${pts[0][0]} ${pts[0][1]} C ${pts[1][0]} ${pts[1][1]}, ${pts[2][0]} ${pts[2][1]}, ${pts[3][0]} ${pts[3][1]} S ${pts[4][0]+10} ${pts[4][1]-10}, ${pts[4][0]} ${pts[4][1]}`;
  // Animated marker on path via dasharray trick
  return (
    <svg viewBox="0 0 260 120" width="100%" height="auto" style={{ display: 'block' }}>
      <rect x="0" y="0" width="260" height="120" fill="rgba(10,10,10,0.4)" />
      {/* Axes */}
      <g stroke="rgba(166,166,166,0.3)" strokeWidth="0.5">
        <line x1="20" y1="100" x2="245" y2="100" />
        <line x1="20" y1="100" x2="20" y2="15" />
        {/* tick marks */}
        {[40, 80, 120, 160, 200, 240].map(x => <line key={x} x1={x} y1="100" x2={x} y2="103" />)}
        {[25, 50, 75].map(y => <line key={y} x1="17" y1={y} x2="20" y2={y} />)}
      </g>
      <text x="10" y="20" fontFamily="JetBrains Mono, monospace" fontSize="7" fill="rgba(166,166,166,0.6)">y</text>
      <text x="248" y="103" fontFamily="JetBrains Mono, monospace" fontSize="7" fill="rgba(166,166,166,0.6)">x</text>
      <text x="120" y="115" fontFamily="JetBrains Mono, monospace" fontSize="7" fill="rgba(166,166,166,0.4)" letterSpacing="0.5">xₚ</text>

      {/* Handle lines */}
      <g stroke="rgba(166,166,166,0.35)" strokeWidth="0.5" strokeDasharray="2 2">
        <line x1={pts[0][0]} y1={pts[0][1]} x2={pts[1][0]} y2={pts[1][1]} />
        <line x1={pts[3][0]} y1={pts[3][1]} x2={pts[2][0]} y2={pts[2][1]} />
      </g>

      {/* path */}
      <path d={path} fill="none" stroke="#F5F5F5" strokeWidth="1.2"
        style={{ filter: 'drop-shadow(0 0 3px rgba(255,255,255,0.3))' }} />
      {/* control points */}
      {pts.map(([cx, cy], i) => (
        <circle key={i} cx={cx} cy={cy} r="2.2"
          fill={i === 0 || i === 3 || i === 4 ? '#F5F5F5' : 'rgba(10,10,10,1)'}
          stroke="#F5F5F5" strokeWidth="0.8" />
      ))}
      {/* Moving plot head */}
      <g transform={`translate(${20 + t * 220}, 100)`}>
        <line x1="0" y1="0" x2="0" y2="-85" stroke="#6BE6FF" strokeWidth="0.5" strokeDasharray="1 2" opacity="0.6" />
        <circle cx="0" cy="0" r="2" fill="#6BE6FF" style={{ filter: 'drop-shadow(0 0 4px #6BE6FF)' }} />
      </g>
    </svg>
  );
};

/* ============================================================
   Mini illustration: Synthetic Input (3D-ish grid w/ cursor)
   ============================================================ */
const IllSyntheticInput = () => {
  const [t, setT] = useStateS(0);
  useEffectS(() => {
    let raf;
    const start = performance.now();
    const loop = (now) => { setT(((now - start) / 3000) % 1); raf = requestAnimationFrame(loop); };
    raf = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(raf);
  }, []);
  // A curved path
  const d = "M 30 85 C 70 30, 110 100, 150 50 S 220 30, 240 70";
  const pathRef = useRefS(null);
  const [pt, setPt] = useStateS({ x: 30, y: 85 });
  const [len, setLen] = useStateS(0);
  useEffectS(() => {
    if (pathRef.current) setLen(pathRef.current.getTotalLength());
  }, []);
  useEffectS(() => {
    if (pathRef.current && len) {
      const p = pathRef.current.getPointAtLength(t * len);
      setPt({ x: p.x, y: p.y });
    }
  }, [t, len]);
  return (
    <svg viewBox="0 0 260 120" width="100%" height="auto" style={{ display: 'block' }}>
      {/* Perspective grid */}
      <g stroke="rgba(166,166,166,0.18)" strokeWidth="0.4">
        {[0, 1, 2, 3, 4, 5, 6, 7, 8].map(i => {
          const x1 = 20 + i * 28;
          const x2 = 50 + i * 23;
          return <line key={'v' + i} x1={x1} y1="110" x2={x2} y2="20" />;
        })}
        {[0, 1, 2, 3, 4, 5].map(i => {
          const y = 20 + i * 18;
          const xL = 35 + i * 3.5;
          const xR = 250 - i * 3.5;
          return <line key={'h' + i} x1={xL} y1={y} x2={xR} y2={y} />;
        })}
      </g>

      {/* trace path */}
      <path ref={pathRef} d={d} fill="none" stroke="rgba(255,255,255,0.18)" strokeWidth="0.8" strokeDasharray="2 2" />
      <path d={d} fill="none" stroke="#F5F5F5" strokeWidth="1.4"
        pathLength="1" strokeDasharray="1" strokeDashoffset={1 - t}
        style={{ filter: 'drop-shadow(0 0 4px rgba(255,255,255,0.5))' }} />

      {/* trailing dot */}
      <circle cx={pt.x} cy={pt.y} r="2.5" fill="#6BE6FF" style={{ filter: 'drop-shadow(0 0 6px #6BE6FF)' }} />

      {/* cursor arrow */}
      <g transform={`translate(${pt.x - 1}, ${pt.y - 1})`}>
        <path d="M 0 0 L 0 12 L 3.4 9 L 5.2 13.5 L 7.4 12.5 L 5.6 8.2 L 10.5 8.2 Z"
          fill="#F5F5F5" stroke="#0A0A0A" strokeWidth="0.4" strokeLinejoin="round" />
      </g>
    </svg>
  );
};

/* ============================================================
   How It Works — 3 step cards
   ============================================================ */
const HowItWorks = () => {
  const ref = useReveal();
  const steps = [
    {
      n: '01',
      title: 'SVG Path Parsing',
      body: 'Raw vector geometry is parsed into a normalized command stream — M, L, C, Q, A — and resampled into a continuous arc-length curve.',
      ill: <IllParsing />,
      mono: 'commands = parsePath(svg)'
    },
    {
      n: '02',
      title: 'Motion Planning',
      body: 'Bezier handles are evaluated at 1 kHz. A look-ahead planner shapes velocity, acceleration, and jerk to match a natural pen feed.',
      ill: <IllPlanning />,
      mono: 'plan = jerkLimited(curve)'
    },
    {
      n: '03',
      title: 'Synthetic Input',
      body: 'Sub-pixel coordinates are dispatched as native Win32 input — SendInput, WM_POINTER, or the Stylus stack. The OS sees a real pen.',
      ill: <IllSyntheticInput />,
      mono: 'SendInput(plan.next)'
    }
  ];
  return (
    <section id="how" className="container" style={{ paddingTop: 80, paddingBottom: 80 }}>
      <SectionHeader
        num="02 / HOW IT WORKS"
        eyebrow="Pipeline"
        title="From vector to input in three stages."
        body="A three-stage pipeline turns static geometry into real-time, naturally-shaped pointer events. Each stage is deterministic, scriptable, and inspectable."
      />
      <div ref={ref} className="reveal" style={{
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
        gap: 20
      }}>
        {steps.map((s, i) => (
          <div key={i} className="panel" style={{ padding: 0, overflow: 'hidden', position: 'relative' }}>
            <div style={{
              padding: '14px 18px',
              borderBottom: '1px solid var(--border)',
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center'
            }}>
              <span className="mono" style={{ fontSize: 10, color: 'var(--blue)', letterSpacing: '0.2em' }}>STEP {s.n}</span>
              <span className="mono" style={{ fontSize: 10, color: 'var(--ink-dim)', letterSpacing: '0.1em' }}>{s.mono}</span>
            </div>
            <div style={{ padding: 18, background: 'rgba(10,10,10,0.4)' }}>{s.ill}</div>
            <div style={{ padding: '20px 22px 24px' }}>
              <h3 className="heading" style={{ margin: 0, fontSize: 19, fontWeight: 600, color: 'var(--ink)' }}>{s.title}</h3>
              <p style={{ margin: '10px 0 0', fontSize: 13.5, color: 'var(--silver)', lineHeight: 1.6 }}>{s.body}</p>
            </div>
          </div>
        ))}
      </div>

      {/* Pipeline flow arrows */}
      <div className="mono" style={{
        marginTop: 32,
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        gap: 14,
        fontSize: 11,
        color: 'var(--ink-dim)',
        letterSpacing: '0.18em'
      }}>
        <span>SVG PATH</span>
        <span style={{ color: 'var(--blue)' }}>→</span>
        <span>MOTION PLANNER</span>
        <span style={{ color: 'var(--blue)' }}>→</span>
        <span>SYNTHETIC INPUT</span>
      </div>
    </section>
  );
};

Object.assign(window, {
  useReveal, SectionHeader, WhatItIs, HowItWorks,
  IllParsing, IllPlanning, IllSyntheticInput
});
