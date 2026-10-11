import { useState } from "react";
import { Icon } from "./Icon.js";
import { cx } from "./format.js";
import { matchPatients } from "./search.js";
import type { PatientRow } from "./types.js";

export interface PatientSearchProps {
  /** The practice's patients to search. The top five matches show, in this order. */
  patients: PatientRow[];
  /** Picking a match (click, or Enter). */
  onOpen?: (patient: PatientRow) => void;
  /** "Search all patients", or Enter with no single match: go to the Patients list filtered by the text. */
  onSearchAll?: (text: string) => void;
  /** Text already in the box, with its dropdown open (for a static mockup of the search in use). */
  defaultQuery?: string;
}

/**
 * The quick patient search at the top of the Today screen (Ctrl K), with a dropdown of the top five matches.
 * Mirrors Shared/PatientSearch.razor.
 */
export function PatientSearch({ patients, onOpen, onSearchAll, defaultQuery = "" }: PatientSearchProps) {
  const [query, setQuery] = useState(defaultQuery);
  const [open, setOpen] = useState(defaultQuery !== "");
  const [active, setActive] = useState(-1);
  const text = query.trim();
  const results = text.length >= 2 ? matchPatients(patients, text).slice(0, 5) : [];

  const onKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "ArrowDown" && results.length > 0) setActive((active + 1) % results.length);
    else if (e.key === "ArrowUp" && results.length > 0) setActive(active <= 0 ? results.length - 1 : active - 1);
    else if (e.key === "Enter") {
      if (active >= 0 && active < results.length) onOpen?.(results[active]);
      else if (results.length === 1) onOpen?.(results[0]);
      else onSearchAll?.(text);
    } else if (e.key === "Escape") setOpen(false);
  };

  return (
    <div className="vd-search-box">
      <Icon name="search" size={18} />
      <input id="vd-patient-search" type="search" autoComplete="off" placeholder="Search patients by name, owner, phone or microchip"
        aria-label="Search patients" value={query} onChange={e => { setQuery(e.target.value); setOpen(true); setActive(-1); }}
        onKeyDown={onKeyDown} onFocus={() => setOpen(true)} onBlur={() => setTimeout(() => setOpen(false), 150)} />
      <kbd>Ctrl K</kbd>
      {open && text.length >= 2 && (
        <div className="vd-dropdown" role="listbox">
          {results.length === 0 && <div className="vd-dropdown-item vd-muted">No patients match “{text}”.</div>}
          {results.map((r, i) => (
            <button key={r.id} type="button" className={cx("vd-dropdown-item", i === active && "active")} role="option" aria-selected={i === active ? "true" : "false"}
              onMouseDown={() => onOpen?.(r)}>
              <span className="vd-avatar">{r.name.slice(0, 1)}</span>
              <span style={{ flex: 1, minWidth: 0 }}>
                <span className="vd-strong">{r.name}</span> <span className="vd-muted">· {r.species}</span>
                <span className="vd-cell-sub" style={{ display: "block" }}>{r.ownerName} · {r.ownerPhone}</span>
              </span>
              <span className="vd-mono vd-small vd-muted">{r.patientNumber}</span>
            </button>
          ))}
          <button type="button" className="vd-dropdown-foot" onMouseDown={() => onSearchAll?.(text)}>Search all patients <Icon name="chevron-right" size={16} /></button>
        </div>
      )}
    </div>
  );
}
