import { fmtDate } from "./format.js";

export interface WeightEntry {
  /** When it was recorded (ISO timestamp, UTC). */
  recordedUtc: string;
  /** Weight in kilograms. */
  kg: number;
  /** Where it came from, e.g. "Consultation" or "Nurse clinic". */
  context: string;
}

export interface WeightChartProps {
  /** Weights, newest first (the list shows the first five; the chart plots them all, oldest on the left). */
  weights: WeightEntry[];
}

const W = 280, H = 70, Pad = 6;
// C#'s "0.#" with the invariant culture.
const f = (v: number) => String(Math.round(v * 10) / 10);
// C#'s "0.0#".
const kg = (v: number) => { const s = String(Math.round(v * 100) / 100); return s.includes(".") ? s : `${s}.0`; };

/** A small line chart of weights over time with the latest five listed under it. Mirrors Shared/WeightChart.razor. */
export function WeightChart({ weights }: WeightChartProps) {
  const ordered = [...weights].sort((a, b) => Date.parse(a.recordedUtc) - Date.parse(b.recordedUtc));
  const points: [number, number][] = [];
  if (ordered.length > 0) {
    const minT = Date.parse(ordered[0].recordedUtc);
    const spanT = Math.max(1, Date.parse(ordered[ordered.length - 1].recordedUtc) - minT);
    const minKg = Math.min(...ordered.map(w => w.kg));
    const spanKg = Math.max(0.5, Math.max(...ordered.map(w => w.kg)) - minKg);
    for (const w of ordered) {
      const x = ordered.length === 1 ? W / 2 : Pad + (W - 2 * Pad) * (Date.parse(w.recordedUtc) - minT) / spanT;
      const y = H - Pad - (H - 2 * Pad) * (w.kg - minKg) / spanKg;
      points.push([x, y]);
    }
  }
  const last = points[points.length - 1];
  return (
    <>
      <svg viewBox={`0 0 ${W} ${H}`} width="100%" height={H} role="img" aria-label="Weight over time" style={{ marginTop: 8, display: "block" }}>
        <line x1="0" y1={H - 1} x2={W} y2={H - 1} stroke="var(--vd-border)" />
        <polyline fill="none" stroke="var(--vd-accent)" strokeWidth="2" points={points.map(([x, y]) => `${f(x)},${f(y)}`).join(" ")} />
        {last && <circle cx={f(last[0])} cy={f(last[1])} r="4" fill="var(--vd-accent)" />}
      </svg>
      <ul className="vd-list vd-small" style={{ marginTop: 8 }}>
        {weights.slice(0, 5).map((w, i) => (
          <li key={i} className="vd-row-between" style={{ padding: "6px 0" }}>
            <span className="vd-muted">{fmtDate(w.recordedUtc)} · {w.context}</span>
            <span className="vd-mono vd-nowrap">{kg(w.kg)} kg</span>
          </li>
        ))}
      </ul>
    </>
  );
}
