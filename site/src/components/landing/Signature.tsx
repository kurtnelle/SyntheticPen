import { useEffect, useRef, useState } from 'react';

export function BackgroundSplines() {
  return (
    <svg
      aria-hidden="true"
      style={{ position: 'absolute', inset: 0, width: '100%', height: '100%', pointerEvents: 'none', zIndex: 0, opacity: 0.5 }}
      viewBox="0 0 1440 900"
      preserveAspectRatio="xMidYMid slice"
    >
      <defs>
        <linearGradient id="splineGrad" x1="0%" y1="0%" x2="100%" y2="0%">
          <stop offset="0%" stopColor="#4DA3FF" stopOpacity="0" />
          <stop offset="50%" stopColor="#4DA3FF" stopOpacity="0.25" />
          <stop offset="100%" stopColor="#6BE6FF" stopOpacity="0" />
        </linearGradient>
      </defs>
      <path d="M -100 200 C 200 100, 400 350, 700 300 S 1200 450, 1600 250" fill="none" stroke="url(#splineGrad)" strokeWidth="1" />
      <path d="M -100 600 C 300 700, 600 500, 900 650 S 1300 800, 1600 600" fill="none" stroke="url(#splineGrad)" strokeWidth="1" />
      <path d="M -100 400 C 250 350, 500 600, 850 500 S 1300 350, 1600 450" fill="none" stroke="rgba(107,230,255,0.12)" strokeWidth="0.8" />
      {([[200, 100], [400, 350], [700, 300], [1200, 450]] as const).map(([x, y], i) => (
        <g key={i}>
          <circle cx={x} cy={y} r="2" fill="#4DA3FF" opacity="0.4" />
          <circle cx={x} cy={y} r="6" fill="none" stroke="#4DA3FF" strokeWidth="0.5" opacity="0.25" />
        </g>
      ))}
    </svg>
  );
}

const SIGNATURE_STROKES = [
  'M 60 175 C 75 115 105 70 140 95 C 165 113 158 155 145 170 C 130 188 108 182 118 160 C 132 130 165 118 195 135 C 215 148 215 170 205 178 C 192 188 188 170 205 168 C 225 165 235 178 245 178 C 262 178 268 138 290 138 C 308 138 312 168 298 178 C 282 188 275 170 295 168 C 318 168 332 180 348 180 C 365 180 370 138 392 138 C 410 138 412 168 398 178 C 382 188 376 170 396 168 C 418 168 430 180 445 180',
  'M 320 128 L 358 128',
  'M 470 105 L 470 180 M 460 140 L 500 140 M 520 158 C 518 144 530 132 545 138 C 558 144 560 168 545 178 C 530 184 520 172 525 162 M 575 178 L 575 138 C 575 138 580 130 595 132 C 608 134 615 145 615 158 L 615 180 M 645 178 C 632 178 627 162 632 148 C 638 134 658 134 665 145 M 695 105 L 695 180 M 695 150 C 700 138 715 132 728 138 C 740 144 740 158 740 168 L 740 180',
];

export function SignatureCanvas() {
  const pathRefs = useRef<(SVGPathElement | null)[]>([]);
  const [lengths, setLengths] = useState<number[]>([]);
  const [progress, setProgress] = useState(0);
  const [coords, setCoords] = useState({ x: 60, y: 175 });
  const rafRef = useRef<number>(0);

  useEffect(() => {
    setLengths(pathRefs.current.map((p) => (p ? p.getTotalLength() : 0)));
  }, []);

  useEffect(() => {
    if (lengths.length === 0) return;
    const totalLen = lengths.reduce((a, b) => a + b, 0);
    const DURATION = 5400;
    const HOLD = 1600;
    const start = performance.now();

    const tick = (t: number) => {
      const elapsed = t - start;
      const cycle = DURATION + HOLD;
      const phase = elapsed % cycle;
      let p = Math.min(1, phase / DURATION);
      p = p < 0.5 ? 4 * p * p * p : 1 - Math.pow(-2 * p + 2, 3) / 2;
      setProgress(p);

      const targetLen = p * totalLen;
      let acc = 0;
      let strokeIdx = 0;
      let localLen = targetLen;
      for (let i = 0; i < lengths.length; i++) {
        if (acc + lengths[i] >= targetLen) {
          strokeIdx = i;
          localLen = targetLen - acc;
          break;
        }
        acc += lengths[i];
      }
      const path = pathRefs.current[strokeIdx];
      if (path) {
        const pt = path.getPointAtLength(Math.max(0, Math.min(localLen, lengths[strokeIdx])));
        setCoords({ x: pt.x, y: pt.y });
      }
      rafRef.current = requestAnimationFrame(tick);
    };
    rafRef.current = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(rafRef.current);
  }, [lengths]);

  const totalLen = lengths.reduce((a, b) => a + b, 0) || 1;
  const targetLen = progress * totalLen;
  let acc = 0;
  const dashOffsets = lengths.map((len) => {
    const visible = Math.max(0, Math.min(len, targetLen - acc));
    acc += len;
    return len - visible;
  });

  return (
    <div style={{ position: 'relative', width: '100%' }}>
      <svg viewBox="0 0 820 260" preserveAspectRatio="xMidYMid meet" style={{ width: '100%', height: 'auto', display: 'block', overflow: 'visible' }}>
        <defs>
          <linearGradient id="inkGrad" x1="0%" y1="0%" x2="100%" y2="0%">
            <stop offset="0%" stopColor="#4DA3FF" />
            <stop offset="100%" stopColor="#6BE6FF" />
          </linearGradient>
          <filter id="inkGlow" x="-20%" y="-20%" width="140%" height="140%">
            <feGaussianBlur stdDeviation="3.5" result="b1" />
            <feGaussianBlur stdDeviation="8" result="b2" in="SourceGraphic" />
            <feMerge>
              <feMergeNode in="b2" />
              <feMergeNode in="b1" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
          <pattern id="microGrid" x="0" y="0" width="20" height="20" patternUnits="userSpaceOnUse">
            <path d="M 20 0 L 0 0 0 20" fill="none" stroke="rgba(77, 163, 255, 0.08)" strokeWidth="0.5" />
          </pattern>
          <pattern id="macroGrid" x="0" y="0" width="100" height="100" patternUnits="userSpaceOnUse">
            <path d="M 100 0 L 0 0 0 100" fill="none" stroke="rgba(77, 163, 255, 0.18)" strokeWidth="0.6" />
          </pattern>
        </defs>

        <rect x="40" y="20" width="740" height="220" fill="url(#microGrid)" />
        <rect x="40" y="20" width="740" height="220" fill="url(#macroGrid)" />
        <rect x="40" y="20" width="740" height="220" fill="none" stroke="rgba(255,255,255,0.06)" strokeWidth="0.6" />

        <g fontFamily="JetBrains Mono, monospace" fontSize="9" fill="rgba(166,166,166,0.55)">
          <text x="46" y="34">200</text>
          <text x="46" y="244">0</text>
          <text x="760" y="244">800</text>
          <line x1="40" y1="240" x2="40" y2="20" stroke="rgba(166,166,166,0.25)" strokeWidth="0.6" />
          <line x1="40" y1="240" x2="780" y2="240" stroke="rgba(166,166,166,0.25)" strokeWidth="0.6" />
          {[0, 1, 2, 3, 4, 5, 6, 7].map((i) => (
            <line key={i} x1={40 + i * 100} y1="240" x2={40 + i * 100} y2="244" stroke="rgba(166,166,166,0.25)" strokeWidth="0.6" />
          ))}
        </g>

        <g filter="url(#inkGlow)" opacity="0.9">
          {SIGNATURE_STROKES.map((d, i) => (
            <path key={'g' + i} d={d} fill="none" stroke="url(#inkGrad)" strokeWidth="3.2" strokeLinecap="round" strokeLinejoin="round" strokeDasharray={lengths[i] || 0} strokeDashoffset={dashOffsets[i] ?? lengths[i] ?? 0} />
          ))}
        </g>
        <g>
          {SIGNATURE_STROKES.map((d, i) => (
            <path key={'c' + i} ref={(el) => { pathRefs.current[i] = el; }} d={d} fill="none" stroke="#E8F4FF" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" strokeDasharray={lengths[i] || 0} strokeDashoffset={dashOffsets[i] ?? lengths[i] ?? 0} />
          ))}
        </g>

        {progress < 1 && (
          <circle cx={coords.x} cy={coords.y} r="5" fill="#6BE6FF" opacity="0.9" style={{ filter: 'drop-shadow(0 0 12px #6BE6FF)' }} />
        )}

        <g fontFamily="JetBrains Mono, monospace" fontSize="11" fill="#6BE6FF">
          <rect x="660" y="34" width="116" height="44" fill="rgba(10,10,10,0.7)" stroke="rgba(107,230,255,0.3)" strokeWidth="0.6" />
          <text x="670" y="50" fill="rgba(166,166,166,0.7)" fontSize="8" letterSpacing="1.5">CURSOR</text>
          <text x="670" y="64">X: {coords.x.toFixed(1).padStart(6)}</text>
          <text x="670" y="74">Y: {coords.y.toFixed(1).padStart(6)}</text>
        </g>

        <g transform={`translate(${coords.x - 1}, ${coords.y - 1})`}>
          <path d="M 0 0 L 0 16 L 4.5 12 L 7 18 L 10 16.7 L 7.5 10.8 L 14 10.8 Z" fill="#F5F5F5" stroke="#0A0A0A" strokeWidth="0.5" strokeLinejoin="round" style={{ filter: 'drop-shadow(0 0 6px rgba(107,230,255,0.6))' }} />
        </g>
      </svg>

      <div style={{ display: 'flex', alignItems: 'center', gap: 16, marginTop: 12, padding: '10px 14px', background: 'rgba(18,18,18,0.6)', border: '1px solid var(--border)', fontFamily: 'JetBrains Mono, monospace', fontSize: 11, color: 'var(--silver)', letterSpacing: '0.08em' }}>
        <span style={{ color: 'var(--blue)' }}>● PLOTTING</span>
        <div style={{ flex: 1, height: 2, background: 'rgba(255,255,255,0.06)', position: 'relative' }}>
          <div style={{ position: 'absolute', top: 0, left: 0, height: '100%', width: `${progress * 100}%`, background: 'linear-gradient(90deg, #4DA3FF, #6BE6FF)', boxShadow: '0 0 8px rgba(107,230,255,0.6)' }} />
        </div>
        <span>{(progress * 100).toFixed(1)}%</span>
        <span style={{ color: 'var(--ink-dim)' }}>FEED 4800 mm/min</span>
        <span style={{ color: 'var(--ink-dim)' }}>STROKES {SIGNATURE_STROKES.length}</span>
      </div>
    </div>
  );
}
