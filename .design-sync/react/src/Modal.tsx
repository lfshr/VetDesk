import { useId, type ReactNode } from "react";
import { Icon } from "./Icon.js";
import { cx } from "./format.js";

export interface ModalProps {
  /** Dialog heading, e.g. "New appointment". */
  title: string;
  /** Smaller line under the title, e.g. the patient and owner. */
  subtitle?: string;
  /** The body: usually a `vd-form-grid` of `vd-field`s. */
  children?: ReactNode;
  /** Footer buttons, right-aligned: secondary (Cancel) first, then the primary action. */
  footer?: ReactNode;
  /** Called by the close button and the Escape key. Clicking the backdrop deliberately does nothing. */
  onClose?: () => void;
  /** Width: "narrow", default (""), "wide" or "xwide". */
  size?: "" | "narrow" | "wide" | "xwide";
  /** Body without padding, for a table or list that runs edge to edge. */
  flush?: boolean;
  /** Stack above another open dialog. */
  onTop?: boolean;
}

/**
 * A dialog in the VetDesk style over a dimmed backdrop: title (and subtitle), close button, body, footer.
 * Mirrors Shared/Modal.razor, which every dialog in the app is built from.
 */
export function Modal({ title, subtitle, children, footer, onClose, size = "", flush = false, onTop = false }: ModalProps) {
  const titleId = "dlg-" + useId().replace(/[^a-zA-Z0-9]/g, "");
  const sizeClass = size === "wide" ? "vd-modal-wide" : size === "xwide" ? "vd-modal-xwide" : size === "narrow" ? "vd-modal-narrow" : "";
  return (
    <div className={cx("vd-overlay", onTop && "vd-overlay-top")} onKeyDown={e => { if (e.key === "Escape") onClose?.(); }}>
      <div className={cx("vd-modal", sizeClass)} role="dialog" aria-modal="true" aria-labelledby={titleId}>
        <div className="vd-modal-head">
          <div>
            <h2 id={titleId}>{title}</h2>
            {subtitle != null && <div className="vd-modal-sub">{subtitle}</div>}
          </div>
          <button type="button" className="vd-icon-btn" aria-label="Close" onClick={onClose}><Icon name="close" size={18} /></button>
        </div>
        <div className={cx("vd-modal-body", flush && "vd-modal-body-flush")}>{children}</div>
        {footer != null && <div className="vd-modal-foot">{footer}</div>}
      </div>
    </div>
  );
}
