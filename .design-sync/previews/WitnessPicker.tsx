import { WitnessPicker } from "@vetdesk/ui";

const witnesses = [
  { id: 2, shortName: "Janet", jobTitle: "Registered veterinary nurse" },
  { id: 3, shortName: "Lorna", jobTitle: "Registered veterinary nurse" },
  { id: 4, shortName: "Douglas", jobTitle: "Veterinary surgeon" },
];

export const Empty = () => (
  <div style={{ width: 520 }}><WitnessPicker witnesses={witnesses} /></div>
);

// The witness step when recording a controlled drug.
export const InDispensing = () => (
  <section className="vd-card vd-card-body" style={{ width: 560 }}>
    <h2 className="vd-section-title">Dispense controlled drug</h2>
    <div className="vd-muted vd-small" style={{ marginBottom: 8 }}>Methadone 10 mg/ml injection <span className="vd-cd">CD</span> · 0.4 ml for Biscuit</div>
    <WitnessPicker id="cd" witnesses={witnesses} witnessId={2} pin="12" />
  </section>
);
