import { useEffect, useRef } from "react";
import { Icon } from "./Icon.js";
import { cx } from "./format.js";

export interface ConfirmDialogProps {
  /** The question, e.g. "Are you sure you want to delete “Bites when restrained”?". */
  title: string;
  /** Optional detail under the question. */
  message?: string | null;
  /** Main button label. Defaults to "OK". */
  confirmLabel?: string;
  /** Secondary button label. Defaults to "Cancel"; pass null for a single-button alert. */
  cancelLabel?: string | null;
  /** Destructive: red button and a bin icon instead of the teal info icon. */
  danger?: boolean;
  /** Main button. */
  onConfirm?: () => void;
  /** Secondary button or Escape. */
  onCancel?: () => void;
}

/**
 * The in-app confirm/alert box (VetDesk never uses browser message boxes), focused on the main button.
 * Mirrors Shared/ConfirmHost.razor, which shows whatever DialogService.ConfirmAsync / AlertAsync asked.
 */
export function ConfirmDialog({ title, message, confirmLabel = "OK", cancelLabel = "Cancel", danger = false, onConfirm, onCancel }: ConfirmDialogProps) {
  const confirm = useRef<HTMLButtonElement>(null);
  useEffect(() => { confirm.current?.focus(); }, []);
  return (
    <div className="vd-overlay vd-overlay-top" onKeyDown={e => { if (e.key === "Escape") onCancel?.(); }}>
      <div className="vd-modal vd-modal-narrow" role="alertdialog" aria-modal="true" aria-labelledby="vd-confirm-title">
        <div className="vd-confirm">
          <span className={cx("vd-confirm-icon", !danger && "vd-confirm-icon-info")}><Icon name={danger ? "trash" : "info"} size={18} /></span>
          <div>
            <h2 id="vd-confirm-title">{title}</h2>
            {message != null && <p>{message}</p>}
          </div>
        </div>
        <div className="vd-modal-foot">
          {cancelLabel != null && <button className="vd-btn vd-btn-secondary" onClick={onCancel}>{cancelLabel}</button>}
          <button className={cx("vd-btn", danger ? "vd-btn-danger" : "vd-btn-primary")} onClick={onConfirm} ref={confirm}>{confirmLabel}</button>
        </div>
      </div>
    </div>
  );
}
