export interface VitalProps {
  /** Field label, e.g. "Heart rate". */
  label: string;
  /** Unit shown inside the right edge of the box, e.g. "bpm" or "°C". */
  unit?: string;
  /** The input's id (labels point at it). */
  id: string;
  /** Current value. */
  value?: string | null;
  /** Read-only, e.g. on a completed (locked) consultation. */
  disabled?: boolean;
  /** Free text (e.g. mucous membranes "Pink, moist") rather than a number. */
  text?: boolean;
  /** Called with the new value when the box loses focus after an edit. */
  onChange?: (value: string) => void;
}

/** One vital sign box on the consultation note: label, value, unit. Mirrors Shared/Vital.razor. */
export function Vital({ label, unit = "", id, value, disabled = false, text = false, onChange }: VitalProps) {
  return (
    <div className="vd-field">
      <label htmlFor={id}>{label}</label>
      <div style={{ position: "relative" }}>
        <input id={id} className="vd-mono" style={{ width: "100%", paddingRight: unit ? "40px" : "12px" }} inputMode={text ? "text" : "decimal"}
          defaultValue={value ?? undefined} onBlur={e => { if (e.currentTarget.value !== (value ?? "")) onChange?.(e.currentTarget.value); }}
          disabled={disabled} maxLength={40} />
        {unit && (
          <span className="vd-small vd-muted" style={{ position: "absolute", right: 10, top: "50%", transform: "translateY(-50%)", pointerEvents: "none" }}>{unit}</span>
        )}
      </div>
    </div>
  );
}
