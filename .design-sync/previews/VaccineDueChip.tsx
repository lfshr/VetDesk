import { VaccineDueChip } from "@vetdesk/ui";

export const States = () => (
  <div className="vd-row vd-wrap">
    <VaccineDueChip dueDate="2027-03-14" />
    <VaccineDueChip dueDate="2026-11-02" isDueSoon />
    <VaccineDueChip dueDate="2026-09-21" isOverdue />
    <VaccineDueChip />
  </div>
);

// The vaccinations table on the patient record.
export const InVaccinations = () => (
  <section className="vd-card" style={{ width: 640 }}>
    <table className="vd-table">
      <thead><tr><th>Vaccine</th><th>Given</th><th>By</th><th>Batch</th><th>Booster</th></tr></thead>
      <tbody>
        <tr><td className="vd-strong">DHP</td><td>14 Mar 2026</td><td>Heather</td><td className="vd-mono">A2391K</td><td><VaccineDueChip dueDate="2027-03-14" /></td></tr>
        <tr><td className="vd-strong">Leptospirosis L4</td><td>2 Nov 2025</td><td>Janet</td><td className="vd-mono">L4-7781</td><td><VaccineDueChip dueDate="2026-11-02" isDueSoon /></td></tr>
        <tr><td className="vd-strong">Kennel cough</td><td>21 Sept 2025</td><td>Douglas</td><td className="vd-mono">—</td><td><VaccineDueChip dueDate="2026-09-21" isOverdue /></td></tr>
      </tbody>
    </table>
  </section>
);
