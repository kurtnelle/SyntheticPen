import { LogoMark } from './icons/LogoMark';

export default function Footer() {
  return (
    <footer style={{
      borderTop: '1px solid var(--border)',
      padding: '40px 0',
      marginTop: 40
    }}>
      <div className="container" style={{
        display: 'flex', justifyContent: 'space-between', alignItems: 'center',
        gap: 20, flexWrap: 'wrap'
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <LogoMark size={18} />
          <span style={{ fontFamily: 'var(--font-display)', fontSize: 14 }}>SyntheticPen</span>
        </div>
        <span className="mono" style={{
          fontSize: 11, color: 'var(--ink-dim)', letterSpacing: '0.15em', textTransform: 'uppercase'
        }}>© 2026 · Built for precision</span>
        <nav style={{ display: 'flex', gap: 20 }}>
          <a className="mono" style={{ fontSize: 11, color: 'var(--silver)' }} href="/docs/getting-started">Docs</a>
          <a className="mono" style={{ fontSize: 11, color: 'var(--silver)' }} href="https://github.com">GitHub</a>
          <a className="mono" style={{ fontSize: 11, color: 'var(--silver)' }} href="/privacy">Privacy</a>
        </nav>
      </div>
    </footer>
  );
}
