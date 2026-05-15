import { useEffect, useRef, useState } from 'react';
import { HERO_DRAW_MS, HERO_HOLD_MS } from '../lib/timing';
import { CursorArrow } from './icons/CursorArrow';

// Decorative cursive path used for the hero draw-on animation.
const SIG_PATH = 'M 120 160 C 140 100 200 100 220 160 C 240 220 300 220 320 160 C 340 100 400 100 420 160';

export default function SignatureCanvas() {
  const pathRef = useRef<SVGPathElement | null>(null);
  const [progress, setProgress] = useState(0);
  const [cursor, setCursor] = useState({ x: 120, y: 160 });
  const [length, setLength] = useState(0);

  useEffect(() => {
    if (pathRef.current) setLength(pathRef.current.getTotalLength());
  }, []);

  useEffect(() => {
    if (!length || !pathRef.current) return;
    let raf = 0;
    const start = performance.now();
    const total = HERO_DRAW_MS + HERO_HOLD_MS;
    const tick = (now: number) => {
      const t = ((now - start) % total) / total;
      const drawT = Math.min(1, t * (total / HERO_DRAW_MS));
      const eased = drawT < 0.5
        ? 4 * drawT * drawT * drawT
        : 1 - Math.pow(-2 * drawT + 2, 3) / 2;
      setProgress(eased);
      const p = pathRef.current!.getPointAtLength(eased * length);
      setCursor({ x: p.x, y: p.y });
      raf = requestAnimationFrame(tick);
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [length]);

  const dashOffset = length * (1 - progress);

  return (
    <svg viewBox="0 0 820 260" width="100%" style={{ maxWidth: 960, display: 'block', margin: '52px auto 0' }}>
      <defs>
        <linearGradient id="inkGrad" x1="0" x2="1">
          <stop offset="0%" stopColor="#4DA3FF" />
          <stop offset="100%" stopColor="#6BE6FF" />
        </linearGradient>
        <filter id="inkGlow">
          <feGaussianBlur stdDeviation="3.5" result="b1" />
          <feGaussianBlur stdDeviation="8" result="b2" />
          <feMerge>
            <feMergeNode in="b1" />
            <feMergeNode in="b2" />
            <feMergeNode in="SourceGraphic" />
          </feMerge>
        </filter>
      </defs>
      <rect x="40" y="20" width="740" height="220"
            fill="none" stroke="rgba(255,255,255,0.06)" strokeWidth="0.6" />
      <path ref={pathRef} d={SIG_PATH}
            stroke="url(#inkGrad)" strokeWidth="3.2" fill="none"
            strokeLinecap="round" filter="url(#inkGlow)"
            strokeDasharray={length} strokeDashoffset={dashOffset} opacity={0.9} />
      <path d={SIG_PATH}
            stroke="#E8F4FF" strokeWidth="1.5" fill="none"
            strokeLinecap="round"
            strokeDasharray={length} strokeDashoffset={dashOffset} />
      {progress < 1 && (
        <circle cx={cursor.x} cy={cursor.y} r="5" fill="#6BE6FF"
                style={{ filter: 'drop-shadow(0 0 12px #6BE6FF)' }} />
      )}
      <CursorArrow x={cursor.x} y={cursor.y} />
    </svg>
  );
}
