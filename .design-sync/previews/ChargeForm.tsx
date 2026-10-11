import { ChargeForm } from "@vetdesk/ui";

const meloxicam = { name: "Meloxicam 1.5 mg/ml oral suspension", grossPrice: 14.4, unit: "bottle", isStocked: true, onHand: 23 };

export const Prescription = () => (
  <div style={{ width: 520 }}><ChargeForm product={meloxicam} isPrescription /></div>
);

export const Service = () => (
  <div style={{ width: 520 }}><ChargeForm product={{ name: "Consultation, standard", grossPrice: 48, unit: "each" }} /></div>
);

export const ControlledDrug = () => (
  <div style={{ width: 520 }}><ChargeForm product={{ name: "Methadone 10 mg/ml injection", grossPrice: 4.8, unit: "ml", isControlledDrug: true, isStocked: true, onHand: 18 }} /></div>
);

export const AllergyConflict = () => (
  <div style={{ width: 520 }}>
    <ChargeForm product={{ name: "Amoxicillin 250 mg tablets", grossPrice: 0.84, unit: "tablet", isStocked: true, onHand: 412 }} isPrescription conflicts={["Penicillin"]} />
  </div>
);

export const ConflictNotAVet = () => (
  <div style={{ width: 520 }}>
    <ChargeForm product={{ name: "Amoxicillin 250 mg tablets", grossPrice: 0.84, unit: "tablet", isStocked: true, onHand: 412 }} conflicts={["Penicillin"]} canOverride={false} />
  </div>
);
