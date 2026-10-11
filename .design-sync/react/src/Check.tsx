import { Icon } from "./Icon.js";

export interface CheckProps {
  /** Passed (green tick) or failed (red warning, bold text). */
  ok?: boolean;
  /** What was checked, e.g. "Weight recorded". */
  text: string;
}

/**
 * One line of a pre-flight checklist, e.g. before completing a consultation. Renders an `<li>`, so put it in a
 * `<ul className="vd-list">`. Mirrors Shared/Check.razor.
 */
export function Check({ ok = false, text }: CheckProps) {
  return (
    <li className="vd-row" style={{ gap: 8, padding: "6px 0", border: "none" }}>
      <span style={{ color: ok ? "var(--vd-success-text)" : "var(--vd-danger-text)" }}><Icon name={ok ? "check" : "alert"} size={16} /></span>
      <span className={ok ? "" : "vd-strong"}>{text}</span>
    </li>
  );
}
