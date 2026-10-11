import { Icon } from "@vetdesk/ui";

const names = ["today", "schedule", "paw", "owners", "stethoscope", "pill", "receipt", "chart", "settings", "search", "plus", "back",
  "chevron-left", "chevron-right", "refresh", "alert", "shield", "home", "bell", "close", "check", "trash", "print", "mail", "download",
  "lock", "clock", "info", "warning", "minus", "card", "cash", "bank", "syringe", "truck", "clipboard", "label", "sign-out", "file", "weight", "edit"] as const;

export const AllIcons = () => (
  <div style={{ display: "grid", gridTemplateColumns: "repeat(7, 96px)", gap: 8 }}>
    {names.map(n => (
      <div key={n} className="vd-card" style={{ display: "grid", placeItems: "center", gap: 6, padding: "12px 4px" }}>
        <Icon name={n} size={24} />
        <span className="vd-small vd-muted">{n}</span>
      </div>
    ))}
  </div>
);

export const InButtons = () => (
  <div className="vd-actions">
    <button type="button" className="vd-btn vd-btn-primary"><Icon name="plus" size={18} /> New appointment</button>
    <button type="button" className="vd-btn vd-btn-secondary"><Icon name="print" size={18} /> Print label</button>
    <button type="button" className="vd-btn vd-btn-danger-outline vd-btn-sm"><Icon name="trash" size={14} /> Delete</button>
    <button type="button" className="vd-icon-btn" aria-label="Close"><Icon name="close" size={18} /></button>
  </div>
);

export const Coloured = () => (
  <div className="vd-row" style={{ gap: 16 }}>
    <span style={{ color: "var(--vd-accent)" }}><Icon name="stethoscope" size={28} /></span>
    <span style={{ color: "var(--vd-success-text)" }}><Icon name="check" size={28} /></span>
    <span style={{ color: "var(--vd-warning-text)" }}><Icon name="clock" size={28} /></span>
    <span style={{ color: "var(--vd-danger-text)" }}><Icon name="alert" size={28} /></span>
    <span style={{ color: "var(--vd-surgery-text)" }}><Icon name="syringe" size={28} /></span>
  </div>
);
