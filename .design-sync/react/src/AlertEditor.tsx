import { useState } from "react";
import { Icon } from "./Icon.js";
import { ConfirmDialog } from "./ConfirmDialog.js";
import type { AlertSeverity } from "./AlertChip.js";

export interface AlertDraft {
  /** Set when editing a saved alert (shows Delete); leave out for a new one. */
  id?: number | null;
  /** Which preset it came from ("0"–"12"), "custom" for typed-in text, or null to start at the pick-list. */
  presetKey?: string | null;
  text: string;
  severity: AlertSeverity;
}

export interface AlertEditorProps {
  alert: AlertDraft;
  onClose?: () => void;
  onSave?: (alert: AlertDraft) => void;
  onDelete?: (id: number) => void;
}

/** The pick-list in the app's alert editor (PatientAlert.Presets in VetDesk.Domain). */
export const alertPresets: { key: string; text: string; severity: AlertSeverity }[] = [
  { key: "0", text: "Aggressive — handle with care", severity: "Alert" },
  { key: "1", text: "Bites when restrained", severity: "Alert" },
  { key: "2", text: "Diabetic — on insulin", severity: "Alert" },
  { key: "3", text: "Epileptic", severity: "Alert" },
  { key: "4", text: "Heart murmur — anaesthetic risk", severity: "Alert" },
  { key: "5", text: "Nervous — muzzle for nail clips", severity: "Warning" },
  { key: "6", text: "Brachycephalic — airway risk", severity: "Warning" },
  { key: "7", text: "Escape risk — keep doors closed", severity: "Warning" },
  { key: "8", text: "Payment required upfront", severity: "Warning" },
  { key: "9", text: "Insured", severity: "Note" },
  { key: "10", text: "Prefers a female vet", severity: "Note" },
  { key: "11", text: "Owner hard of hearing", severity: "Note" },
  { key: "12", text: "Microchip details need updating", severity: "Note" },
];
const severities: AlertSeverity[] = ["Alert", "Warning", "Note"];

/**
 * Edits one patient alert: pick a preset or write your own, then choose how serious it is. Deleting asks for
 * confirmation first. Mirrors Shared/AlertEditor.razor.
 */
export function AlertEditor({ alert, onClose, onSave, onDelete }: AlertEditorProps) {
  const [preset, setPreset] = useState<string | null>(alert.presetKey ?? null);
  const [text, setText] = useState(alert.text);
  const [severity, setSeverity] = useState<AlertSeverity>(alert.severity);
  const [confirming, setConfirming] = useState(false);
  const isNew = alert.id == null;

  const pickPreset = (key: string) => {
    if (!key) return;
    setPreset(key);
    const p = alertPresets.find(x => x.key === key);
    if (p) { setText(p.text); setSeverity(p.severity); } else setText("");
  };

  return (
    <div className="vd-card vd-card-body" style={{ marginTop: 12, maxWidth: 520 }}>
      <div className="vd-row-between"><h3 className="vd-section-title" style={{ margin: 0 }}>{isNew ? "New alert" : "Edit alert"}</h3></div>
      {preset == null ? (
        <div className="vd-field">
          <label htmlFor="alert-preset">Alert</label>
          <select id="alert-preset" onChange={e => pickPreset(e.target.value)} defaultValue="">
            <option value="">Choose an alert…</option>
            {alertPresets.map(p => <option key={p.key} value={p.key}>{p.text}</option>)}
            <option value="custom">Something else (write your own)</option>
          </select>
        </div>
      ) : (
        <>
          <div className="vd-field">
            <div className="vd-row-between">
              <label htmlFor="alert-text">Alert</label>
              {isNew && <button type="button" className="vd-btn-link vd-small" onClick={() => setPreset(null)}>Choose another</button>}
            </div>
            <input id="alert-text" value={text} onChange={e => setText(e.target.value)} maxLength={200} />
          </div>
          <div className="vd-field">
            <span className="vd-label">Severity</span>
            <div className="vd-segmented" role="group" aria-label="Severity">
              {severities.map(s => <button key={s} type="button" aria-pressed={severity === s ? "true" : "false"} onClick={() => setSeverity(s)}>{s}</button>)}
            </div>
          </div>
        </>
      )}
      <div className="vd-row" style={{ marginTop: 14 }}>
        {!isNew && <button type="button" className="vd-btn vd-btn-danger-outline vd-btn-sm" onClick={() => setConfirming(true)}><Icon name="trash" size={14} /> Delete</button>}
        <span className="vd-spacer"></span>
        <button type="button" className="vd-btn vd-btn-secondary vd-btn-sm" onClick={onClose}>Cancel</button>
        <button type="button" className="vd-btn vd-btn-primary vd-btn-sm" disabled={preset == null || text.trim() === ""}
          onClick={() => onSave?.({ id: alert.id ?? null, presetKey: preset, text, severity })}>{isNew ? "Add alert" : "Save"}</button>
      </div>
      {confirming && (
        <ConfirmDialog title={`Are you sure you want to delete “${alert.text}”?`} confirmLabel="Delete" danger
          onCancel={() => setConfirming(false)} onConfirm={() => { setConfirming(false); onDelete?.(alert.id!); }} />
      )}
    </div>
  );
}
