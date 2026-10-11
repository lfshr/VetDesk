import { AlertChip } from "@vetdesk/ui";

export const Severities = () => (
  <div className="vd-row vd-wrap">
    <AlertChip text="Bites when restrained" severity="Alert" />
    <AlertChip text="Nervous — muzzle for nail clips" severity="Warning" />
    <AlertChip text="Insured" severity="Note" />
    <AlertChip text="Penicillin" isAllergy />
  </div>
);

// The alerts row under the patient's name on the record (Pages/PatientRecord.razor).
export const PatientHeader = () => (
  <div style={{ width: 620 }}>
    <div className="vd-row" style={{ gap: 14 }}>
      <span className="vd-avatar vd-avatar-lg">B</span>
      <div>
        <h1 className="vd-page-title">Biscuit</h1>
        <div className="vd-muted">Dog · Border Terrier · Male neutered · 6 years</div>
        <div className="vd-muted vd-small" style={{ marginTop: 2 }}>Chip <span className="vd-mono">985112004567812</span></div>
      </div>
    </div>
    <div className="vd-row" style={{ gap: 6, marginTop: 10, flexWrap: "wrap" }} aria-label="Alerts">
      <AlertChip text="Penicillin" isAllergy large />
      <AlertChip text="Bites when restrained" severity="Alert" large />
      <AlertChip text="Brachycephalic — airway risk" severity="Warning" large />
      <AlertChip text="Insured" severity="Note" large />
    </div>
  </div>
);
