import { Icon } from "./Icon.js";
import { cx } from "./format.js";

export type ToastKind = "Success" | "Warning" | "Error";

export interface Toast {
  id: string;
  /** Short past-tense confirmation, e.g. "Biscuit checked in". */
  text: string;
  /** Defaults to Success. In the app, errors stay until dismissed; the others go after four seconds. */
  kind?: ToastKind;
}

export interface ToastsProps {
  /** The toasts showing now, oldest first (the app keeps at most four). */
  toasts: Toast[];
  /** The close button on a toast. */
  onDismiss?: (id: string) => void;
}

/** Short confirmations stacked in the corner of the screen. Mirrors Shared/ToastHost.razor. */
export function Toasts({ toasts, onDismiss }: ToastsProps) {
  return (
    <div className="vd-toasts" role="status" aria-live="polite">
      {toasts.map(t => (
        <div key={t.id} className={cx("vd-toast", t.kind === "Error" ? "vd-toast-error" : t.kind === "Warning" ? "vd-toast-warning" : "")}>
          <Icon name={(t.kind ?? "Success") === "Success" ? "check" : "warning"} size={18} />
          <span>{t.text}</span>
          <button type="button" className="vd-icon-btn" aria-label="Dismiss" onClick={() => onDismiss?.(t.id)}><Icon name="close" size={16} /></button>
        </div>
      ))}
    </div>
  );
}
