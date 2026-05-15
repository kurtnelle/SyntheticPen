export function LogoMark({ className = '', size = 22 }: { className?: string; size?: number }) {
  return (
    <svg className={className} width={size} height={size} viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
      <path d="M3 21L9.5 14.5" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" />
      <path d="M9.5 14.5L17 7L20 10L12.5 17.5L9.5 14.5Z" stroke="currentColor" strokeWidth="1.6" strokeLinejoin="round" />
      <path d="M17 7L19 5L21 7L20 10" stroke="currentColor" strokeWidth="1.6" strokeLinejoin="round" />
      <circle cx="9.5" cy="14.5" r="1.4" fill="currentColor" />
    </svg>
  );
}

export function CursorArrow({ size = 22 }: { size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" style={{ filter: 'drop-shadow(0 0 6px rgba(107,230,255,0.7))' }}>
      <path
        d="M3 2 L3 18 L7.5 14 L10 20 L13 18.7 L10.5 12.8 L17 12.8 Z"
        fill="#F5F5F5" stroke="#0A0A0A" strokeWidth="0.6" strokeLinejoin="round"
      />
    </svg>
  );
}
