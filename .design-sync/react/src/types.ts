/** A patient as the app's lists and searches show it (the fields of the app's PatientRow the components use). */
export interface PatientRow {
  id: number;
  /** Pet name, e.g. "Biscuit". */
  name: string;
  /** Practice patient number, e.g. "P-00412". */
  patientNumber?: string;
  /** e.g. "Dog". */
  species: string;
  /** e.g. "Dog · Border Terrier". */
  speciesAndBreed: string;
  /** Owner's full name, e.g. "Sarah Whitfield". */
  ownerName: string;
  ownerPhone?: string;
  microchip?: string | null;
  /** Drug allergies, e.g. ["Penicillin"]: shown as red chips once picked. */
  allergyLabels?: string[];
}

export type ProductCategory = "Service" | "Medicine" | "Vaccine" | "Retail";

/** A price-list entry as product search shows it (the fields of the app's ProductDto the component uses). */
export interface ProductRow {
  id: number;
  /** Price-list code, e.g. "AMX250". */
  code: string;
  name: string;
  category: ProductCategory;
  /** Price per unit including VAT, in pounds. */
  grossPrice: number;
  /** Selling unit, e.g. "tablet". */
  unit: string;
  isControlledDrug?: boolean;
  isStocked?: boolean;
  onHand?: number;
  /** Allergies of the current patient this product conflicts with, e.g. ["Penicillin"]. */
  allergyConflicts?: string[];
}
