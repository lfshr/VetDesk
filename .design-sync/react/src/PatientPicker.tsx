import { useState } from "react";
import { Icon } from "./Icon.js";
import { cx } from "./format.js";
import { matchPatients } from "./search.js";
import type { PatientRow } from "./types.js";

export interface NewClientAndPatient {
  ownerName: string;
  phone: string;
  petName: string;
  species: string;
}

export interface PatientPickerProps {
  /** The chosen patient: shows as a card with a Change button instead of the search. */
  selected?: PatientRow | null;
  /** Picking a patient, or null for Change. */
  onSelectedChange?: (patient: PatientRow | null) => void;
  /** The patient can't be changed (e.g. booking from a patient's record). */
  locked?: boolean;
  /** Defaults to "picker-search". */
  inputId?: string;
  /** The practice's patients to search (up to six matches show, in this order). */
  patients?: PatientRow[];
  /** Shown before anything is typed: the four patients seen most recently. */
  recent?: PatientRow[];
  /** "Add and select" in the new client form. Return the new patient to select it. */
  onAddNew?: (draft: NewClientAndPatient) => PatientRow | void | Promise<PatientRow | void>;
  /** Text already in the box (for a static mockup of the search in use). */
  defaultQuery?: string;
  /** Start with the "New client and patient" form open. */
  defaultNewOpen?: boolean;
}

/** The species choices on the new patient form (Patient.SpeciesOptions in VetDesk.Domain). */
export const speciesOptions = ["Dog", "Cat", "Rabbit", "Guinea pig", "Other"];

/**
 * Picks a patient inside a dialog (e.g. New appointment): search box, recent patients or matches, and an inline
 * "New client and patient" form. Arrow keys and Enter work on whichever list is showing.
 * Mirrors Shared/PatientPicker.razor.
 */
export function PatientPicker({ selected, onSelectedChange, locked = false, inputId = "picker-search", patients = [], recent = [], onAddNew, defaultQuery = "", defaultNewOpen = false }: PatientPickerProps) {
  const [query, setQuery] = useState(defaultQuery);
  const [active, setActive] = useState(-1);
  const [newOpen, setNewOpen] = useState(defaultNewOpen);
  const [adding, setAdding] = useState(false);
  const [owner, setOwner] = useState("");
  const [phone, setPhone] = useState("");
  const [pet, setPet] = useState("");
  const [species, setSpecies] = useState("Dog");
  const searching = query.trim().length >= 2;
  const results = searching ? matchPatients(patients, query).slice(0, 6) : [];
  const shown = searching ? results : recent;
  const canAdd = owner.trim() !== "" && phone.trim() !== "" && pet.trim() !== "";

  if (selected) {
    return (
      <div className="vd-row" style={{ padding: "10px 12px", border: "1px solid var(--vd-border-strong)", borderRadius: "var(--vd-radius-sm)" }}>
        <span className="vd-avatar">{selected.name.slice(0, 1)}</span>
        <div style={{ flex: 1, minWidth: 0 }}>
          <div className="vd-strong">{selected.name}</div>
          <div className="vd-cell-sub">{selected.speciesAndBreed} · {selected.ownerName}</div>
        </div>
        {(selected.allergyLabels ?? []).map(a => <span key={a} className="vd-chip vd-chip-danger">{a}</span>)}
        {!locked && <button type="button" className="vd-btn vd-btn-ghost vd-btn-sm" onClick={() => onSelectedChange?.(null)}>Change</button>}
      </div>
    );
  }

  const onKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "ArrowDown" && shown.length > 0) setActive((active + 1) % shown.length);
    else if (e.key === "ArrowUp" && shown.length > 0) setActive(active <= 0 ? shown.length - 1 : active - 1);
    else if (e.key === "Enter" && shown.length > 0) onSelectedChange?.(shown[Math.max(active, 0)]);
  };

  const addNew = async () => {
    setAdding(true);
    const created = await onAddNew?.({ ownerName: owner, phone, petName: pet, species });
    setAdding(false);
    setNewOpen(false);
    setOwner(""); setPhone(""); setPet("");
    if (created) onSelectedChange?.(created);
  };

  return (
    <>
      <div className="vd-search-box" style={{ maxWidth: "none" }}>
        <Icon name="search" size={18} />
        <input id={inputId} type="search" autoComplete="off" placeholder="Search patient, owner or microchip" aria-label="Search patient"
          value={query} onChange={e => { setQuery(e.target.value); setActive(-1); }} onKeyDown={onKeyDown} style={{ paddingRight: 12 }} />
      </div>
      <div className="vd-card" style={{ marginTop: 6 }}>
        {searching ? (
          <>
            {results.map((r, i) => (
              <button key={r.id} type="button" className={cx("vd-dropdown-item", i === active && "active")} onClick={() => onSelectedChange?.(r)}>
                <span style={{ flex: 1 }}><span className="vd-strong">{r.name}</span> <span className="vd-muted">· {r.speciesAndBreed}</span></span>
                <span className="vd-small vd-muted">{r.ownerName}</span>
              </button>
            ))}
            {results.length === 0 && <div className="vd-dropdown-item vd-muted">No match. Add them as a new client below.</div>}
          </>
        ) : recent.length > 0 ? (
          <>
            <div className="vd-small vd-muted" style={{ padding: "8px 12px 4px" }}>Recent patients</div>
            {recent.map((r, i) => (
              <button key={r.id} type="button" className={cx("vd-dropdown-item", i === active && "active")} style={{ borderTop: "1px solid var(--vd-border)" }} onClick={() => onSelectedChange?.(r)}>
                <span style={{ flex: 1 }}><span className="vd-strong">{r.name}</span><span className="vd-cell-sub" style={{ display: "block" }}>{r.speciesAndBreed}</span></span>
                <span>{r.ownerName}</span>
              </button>
            ))}
          </>
        ) : (
          <div className="vd-dropdown-item vd-muted vd-small">Type at least two letters of the pet's or owner's name, a phone number or a microchip.</div>
        )}
        {/* The app writes aria-expanded as a bare attribute when open (see NOTES.md); mirrored as-is. */}
        <button type="button" className="vd-dropdown-foot" aria-expanded={newOpen ? "" as unknown as boolean : undefined} onClick={() => setNewOpen(!newOpen)}><span><Icon name="plus" size={16} /> New client and patient</span></button>
        {newOpen && (
          <div className="vd-card-body" style={{ borderTop: "1px solid var(--vd-border)" }}>
            <div className="vd-form-grid">
              <div className="vd-field"><label htmlFor="np-owner">Owner name</label><input id="np-owner" value={owner} onChange={e => setOwner(e.target.value)} /></div>
              <div className="vd-field"><label htmlFor="np-phone">Phone</label><input id="np-phone" type="tel" value={phone} onChange={e => setPhone(e.target.value)} /></div>
              <div className="vd-field"><label htmlFor="np-pet">Pet name</label><input id="np-pet" value={pet} onChange={e => setPet(e.target.value)} /></div>
              <div className="vd-field">
                <label htmlFor="np-species">Species</label>
                <select id="np-species" value={species} onChange={e => setSpecies(e.target.value)}>
                  {speciesOptions.map(s => <option key={s}>{s}</option>)}
                </select>
              </div>
            </div>
            <div className="vd-actions vd-actions-end" style={{ marginTop: 12 }}>
              <button type="button" className="vd-btn vd-btn-secondary vd-btn-sm" onClick={() => setNewOpen(false)}>Cancel</button>
              <button type="button" className="vd-btn vd-btn-primary vd-btn-sm" disabled={!canAdd || adding} onClick={addNew}>Add and select</button>
            </div>
          </div>
        )}
      </div>
    </>
  );
}
