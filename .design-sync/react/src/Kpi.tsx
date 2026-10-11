import { cx } from "./format.js";

export interface KpiProps {
  /** What the figure is, e.g. "Takings". */
  label: string;
  /** The figure, already formatted, e.g. "£4,210.00" or "38". */
  value: string;
  /** Percentage change against the previous period. Leave out when there's nothing to compare with. */
  change?: number | null;
}

// C#'s "0.#" format: one decimal at most, rounded half away from zero.
const oneDecimal = (n: number) => String(Math.round(n * 10) / 10);

/** A headline figure card on the Reports screen. Mirrors Shared/Kpi.razor. Put several in a `vd-kpis` row. */
export function Kpi({ label, value, change }: KpiProps) {
  const changeText = change == null ? "No earlier figures to compare"
    : change > 0 ? `+${oneDecimal(change)}% vs previous`
    : change < 0 ? `−${oneDecimal(Math.abs(change))}% vs previous`
    : "No change";
  return (
    <div className="vd-card vd-kpi">
      <div className="vd-kpi-label">{label}</div>
      <div className="vd-kpi-value">{value}</div>
      <div className={cx("vd-kpi-change", change != null && change > 0 ? "vd-up" : change != null && change < 0 ? "vd-down" : "vd-muted")}>{changeText}</div>
    </div>
  );
}
