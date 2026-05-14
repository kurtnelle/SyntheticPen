import SignatureCanvas from './SignatureCanvas';

export default function Hero() {
  return (
    <section style={{ padding: '56px 0 80px', position: 'relative', overflow: 'hidden' }}>
      <div className="container" style={{ textAlign: 'center' }}>
        <span className="mono" style={{
          display: 'inline-flex', alignItems: 'center', gap: 8,
          fontSize: 11, textTransform: 'uppercase', letterSpacing: '0.15em',
          color: 'var(--blue)', background: 'rgba(77,163,255,0.08)',
          border: '1px solid rgba(77,163,255,0.2)', borderRadius: 2, padding: '6px 12px'
        }}>
          <span style={{
            width: 6, height: 6, borderRadius: '50%', background: 'var(--blue)',
            boxShadow: '0 0 8px var(--blue)'
          }} />
          v0.4 BETA · Windows 10/11
        </span>

        <h1 style={{
          fontSize: 'clamp(48px, 7.5vw, 104px)', fontWeight: 700,
          lineHeight: 0.95, letterSpacing: '-0.025em', textTransform: 'uppercase',
          margin: '24px 0 0'
        }}>
          Vector Paths<br />Into Real Motion
        </h1>

        <p style={{ maxWidth: 640, margin: '24px auto 0', color: 'var(--silver)' }}>
          Synthetic cursor & pen motion for Windows. SyntheticPen replays SVG paths as native input — like a CNC plotter for your handwriting.
        </p>

        <SignatureCanvas />

        <div style={{ display: 'flex', gap: 16, justifyContent: 'center', marginTop: 44, flexWrap: 'wrap' }}>
          <a href="#cta" style={{
            fontFamily: 'var(--font-display)', fontWeight: 600,
            background: 'linear-gradient(135deg, #4DA3FF, #6BE6FF)',
            color: '#0A0A0A', padding: '14px 22px', borderRadius: 2,
            boxShadow: '0 0 0 1px rgba(107,230,255,0.4), 0 0 24px rgba(77,163,255,0.35), 0 0 60px rgba(77,163,255,0.15)',
            textTransform: 'uppercase', letterSpacing: '0.05em', fontSize: 14
          }}>Download Beta ↘</a>
          <a href="#how" style={{
            fontFamily: 'var(--font-display)', fontWeight: 500,
            border: '1px solid var(--border-strong)', padding: '14px 22px', borderRadius: 2,
            textTransform: 'uppercase', letterSpacing: '0.05em', fontSize: 14, color: 'var(--ink)'
          }}>See How It Works</a>
        </div>
      </div>
    </section>
  );
}
