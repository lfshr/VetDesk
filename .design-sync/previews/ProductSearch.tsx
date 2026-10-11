import { ProductSearch } from "@vetdesk/ui";

const products = [
  { id: 1, code: "AMX250", name: "Amoxicillin 250 mg tablets", category: "Medicine" as const, grossPrice: 0.84, unit: "tablet", isStocked: true, onHand: 412, allergyConflicts: ["Penicillin"] },
  { id: 2, code: "AMXCL", name: "Amoxicillin/clavulanate 50 mg tablets", category: "Medicine" as const, grossPrice: 1.12, unit: "tablet", isStocked: true, onHand: 96, allergyConflicts: ["Penicillin"] },
  { id: 3, code: "MEL15", name: "Meloxicam 1.5 mg/ml oral suspension", category: "Medicine" as const, grossPrice: 14.4, unit: "bottle", isStocked: true, onHand: 23 },
  { id: 4, code: "METH10", name: "Methadone 10 mg/ml injection", category: "Medicine" as const, grossPrice: 4.8, unit: "ml", isControlledDrug: true, isStocked: true, onHand: 18 },
  { id: 5, code: "CONS", name: "Consultation, standard", category: "Service" as const, grossPrice: 48, unit: "each" },
];

// The dropdown is absolutely positioned; the frame gives it room inside the card.
const frame = (children: React.ReactNode) => <div style={{ width: 560, minHeight: 280 }}>{children}</div>;

export const Empty = () => frame(<ProductSearch products={products} />);

export const AllergyConflicts = () => frame(<ProductSearch products={products} defaultQuery="amox" placeholder="Prescribe a medicine" />);

export const ControlledDrug = () => frame(<ProductSearch products={products} defaultQuery="meth" categories={["Medicine"]} />);

export const NoMatch = () => frame(<ProductSearch products={products} defaultQuery="zz" />);
