export interface Witness {
  id: number;
  /** First name as shown in the app, e.g. "Priya". */
  shortName: string;
  /** e.g. "Veterinary surgeon", "Registered veterinary nurse". */
  jobTitle: string;
}

export interface WitnessPickerProps {
  /** Prefix for the two input ids. Defaults to "witness". */
  id?: string;
  /** Staff who may witness (not including whoever is signed in). */
  witnesses: Witness[];
  witnessId?: number | null;
  onWitnessIdChange?: (id: number | null) => void;
  pin?: string | null;
  onPinChange?: (pin: string) => void;
}

/**
 * A second member of staff confirms a controlled drug entry with their own PIN: a witness select and a PIN box,
 * with a hint underneath. Mirrors Shared/WitnessPicker.razor.
 */
export function WitnessPicker({ id = "witness", witnesses, witnessId, onWitnessIdChange, pin, onPinChange }: WitnessPickerProps) {
  return (
    <>
      <div className="vd-form-grid">
        <div className="vd-field">
          <label htmlFor={`${id}-who`}>Witness</label>
          <select id={`${id}-who`} value={witnessId?.toString() ?? ""} onChange={e => onWitnessIdChange?.(e.target.value ? Number(e.target.value) : null)}>
            <option value="">Choose a witness</option>
            {witnesses.map(s => <option key={s.id} value={s.id}>{s.shortName} ({s.jobTitle})</option>)}
          </select>
        </div>
        <div className="vd-field">
          <label htmlFor={`${id}-pin`}>Witness's PIN</label>
          <input id={`${id}-pin`} type="password" inputMode="numeric" maxLength={4} autoComplete="off" className="vd-mono" value={pin ?? ""} onChange={e => onPinChange?.(e.target.value)} />
        </div>
      </div>
      <div className="vd-hint">The witness types their own PIN to confirm they saw the amount. It isn't stored.</div>
    </>
  );
}
