export function LogoMark({ size = 22 }: { size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24"
         style={{ filter: 'drop-shadow(0 0 6px rgba(77,163,255,0.4))' }}>
      <path d="M4 20 L14 4 L20 8 L10 24 Z" fill="none" stroke="#4DA3FF" strokeWidth="1.5" />
      <circle cx="14" cy="4" r="1.2" fill="#6BE6FF" />
    </svg>
  );
}
