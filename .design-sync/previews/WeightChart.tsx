import { WeightChart } from "@vetdesk/ui";

const weights = [
  { recordedUtc: "2026-10-08T09:20:00Z", kg: 12.4, context: "Consultation" },
  { recordedUtc: "2026-07-14T14:05:00Z", kg: 12.85, context: "Nurse clinic" },
  { recordedUtc: "2026-03-02T10:40:00Z", kg: 13.1, context: "Vaccination" },
  { recordedUtc: "2025-11-19T16:30:00Z", kg: 12, context: "Consultation" },
  { recordedUtc: "2025-06-03T11:00:00Z", kg: 11.6, context: "Consultation" },
  { recordedUtc: "2024-12-01T11:00:00Z", kg: 10.9, context: "Puppy check" },
];

// The weight card in the side column of the patient record.
export const SideCard = () => (
  <section className="vd-card vd-card-body" style={{ width: 300 }}>
    <h2 className="vd-section-title">Weight</h2>
    <WeightChart weights={weights} />
  </section>
);

export const SingleWeight = () => (
  <section className="vd-card vd-card-body" style={{ width: 300 }}>
    <h2 className="vd-section-title">Weight</h2>
    <WeightChart weights={[{ recordedUtc: "2026-10-08T09:20:00Z", kg: 4.2, context: "Consultation" }]} />
  </section>
);
