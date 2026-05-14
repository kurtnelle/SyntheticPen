import { LogoMark } from './icons/LogoMark';

const navItems = [
  { href: '#what', label: 'What it is' },
  { href: '#how', label: 'How it works' },
  { href: '#use', label: 'Use cases' },
  { href: '#tech', label: 'Technology' }
];

export default function Header() {
  return (
    <header style={{
      position: 'sticky', top: 0, zIndex: 10,
      backdropFilter: 'blur(12px)',
      background: 'rgba(10,10,10,0.6)',
      borderBottom: '1px solid var(--border)'
    }}>
      <div className="container" style={{
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        padding: '18px 32px'
      }}>
        <a href="#" style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <LogoMark />
          <span style={{ fontFamily: 'var(--font-display)', fontSize: 17, fontWeight: 600 }}>
            Synthetic<span style={{ color: 'var(--ink-dim)', fontWeight: 400 }}>Pen</span>
          </span>
        </a>
        <nav style={{ display: 'flex', alignItems: 'center', gap: 28 }}>
          {navItems.map(i => (
            <a key={i.href} href={i.href} className="mono" style={{
              fontSize: 11, textTransform: 'uppercase', letterSpacing: '0.15em',
              color: 'var(--silver)'
            }}>{i.label}</a>
          ))}
          <a href="#cta" className="mono" style={{
            fontSize: 11, textTransform: 'uppercase', letterSpacing: '0.15em',
            color: 'var(--blue)', border: '1px solid rgba(77,163,255,0.3)',
            padding: '7px 14px', borderRadius: 2
          }}>Download</a>
        </nav>
      </div>
    </header>
  );
}
