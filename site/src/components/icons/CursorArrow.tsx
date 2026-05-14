export function CursorArrow({ x, y }: { x: number; y: number }) {
  return (
    <g transform={`translate(${x}, ${y})`}
       style={{ filter: 'drop-shadow(0 0 4px rgba(107,230,255,0.7))' }}>
      <path d="M0 0 L0 14 L4 10 L7 16 L9 15 L6 9 L11 9 Z"
            fill="#F5F5F5" stroke="#000" strokeWidth="0.5" />
    </g>
  );
}
