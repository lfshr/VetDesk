// In-memory stand-ins for the app's SearchPatientsQuery / SearchProductsQuery, so the search components work in a
// prototype from a plain array. Matching follows the queries' rules; order is the array's own order.
import type { PatientRow, ProductRow } from "./types.js";

export function matchPatients(patients: PatientRow[], text: string): PatientRow[] {
  const t = text.trim().toLowerCase();
  const compact = t.replace(/\s+/g, "");
  const digits = t.replace(/\D/g, "");
  return patients.filter(p =>
    p.name.toLowerCase().includes(t) ||
    p.ownerName.toLowerCase().includes(t) ||
    (p.microchip != null && p.microchip.toLowerCase().includes(compact)) ||
    (digits.length >= 4 && (p.ownerPhone ?? "").replace(/\D/g, "").includes(digits)));
}

export function matchProducts(products: ProductRow[], text: string): ProductRow[] {
  const t = text.trim().toLowerCase();
  return products.filter(p => p.name.toLowerCase().includes(t) || p.code.toLowerCase().includes(t));
}
