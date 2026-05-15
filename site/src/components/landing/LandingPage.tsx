import { LogoMark } from './icons';
import { BackgroundSplines, SignatureCanvas } from './Signature';
import { WhatItIs, HowItWorks } from './sections';
import { UseCases, Technology, CTA, Footer } from './sections2';

function scrollTo(id: string) {
  document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

function Header() {
  return (
    <header className="nav">
      <div className="nav-inner">
        <div className="logo">
          <LogoMark className="logo-mark" />
          <span>Synthetic<span style={{ color: 'var(--ink-dim)', fontWeight: 400 }}>Pen</span></span>
        </div>
        <nav className="nav-links">
          <a className="nav-link" onClick={() => scrollTo('what')}>What it is</a>
          <a className="nav-link" onClick={() => scrollTo('how')}>How it works</a>
          <a className="nav-link" onClick={() => scrollTo('use')}>Use cases</a>
          <a className="nav-link" onClick={() => scrollTo('tech')}>Technology</a>
          <a className="nav-link cta" onClick={() => scrollTo('cta')}>Download</a>
        </nav>
      </div>
    </header>
  );
}

function Hero() {
  return (
    <section style={{ position: 'relative', paddingTop: 56, paddingBottom: 80, overflow: 'hidden' }}>
      <BackgroundSplines />
      <div className="container" style={{ position: 'relative', zIndex: 2 }}>
        <div style={{ display: 'flex', justifyContent: 'center', marginBottom: 28 }}>
          <span className="tag"><span className="dot" />Free · Windows 10/11</span>
        </div>
        <h1 className="display" style={{ textAlign: 'center', maxWidth: 1100, margin: '0 auto' }}>
          Vector Paths<br />Into Real Motion
        </h1>
        <p className="subhead" style={{ textAlign: 'center', maxWidth: 640, margin: '24px auto 0' }}>
          Synthetic cursor &amp; pen motion for Windows. SyntheticPen replays SVG paths as native input — like a CNC plotter for your handwriting.
        </p>
        <div style={{ maxWidth: 960, margin: '52px auto 0', position: 'relative' }}>
          <SignatureCanvas />
        </div>
        <div style={{ display: 'flex', justifyContent: 'center', gap: 16, marginTop: 44, flexWrap: 'wrap' }}>
          <button className="btn-primary" onClick={() => scrollTo('cta')}>
            Download
            <svg width="12" height="12" viewBox="0 0 12 12" fill="none">
              <path d="M6 1V11M6 11L1 6M6 11L11 6" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
          </button>
          <button className="btn-ghost" onClick={() => scrollTo('how')}>See how it works</button>
        </div>
        <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', gap: 28, marginTop: 48, fontFamily: 'JetBrains Mono, monospace', fontSize: 11, color: 'var(--ink-dim)', letterSpacing: '0.12em', textTransform: 'uppercase' }}>
          <span>Native Win32 input</span>
          <span style={{ opacity: 0.3 }}>/</span>
          <span>SVG · TTF/OTF</span>
        </div>
      </div>
    </section>
  );
}

export default function LandingPage() {
  return (
    <>
      <Header />
      <main>
        <Hero />
        <WhatItIs />
        <HowItWorks />
        <UseCases />
        <Technology />
        <CTA />
      </main>
      <Footer />
    </>
  );
}
