import { useState } from "react";
import { Icon } from "./Icon.js";
import { cx, fmtMoney, fmtQty } from "./format.js";

/** The price-list fields a charge needs (a subset of the app's ProductDto). */
export interface ChargeProduct {
  name: string;
  /** Price per unit including VAT, in pounds. */
  grossPrice: number;
  /** Selling unit, e.g. "tablet", "ml", "each". */
  unit: string;
  /** Controlled drug: shows the CD tag (and needs a witness when dispensed). */
  isControlledDrug?: boolean;
  /** Kept in stock, so the stock level is shown. */
  isStocked?: boolean;
  /** Quantity in stock, in units. */
  onHand?: number;
}

export interface ChargeInput {
  product: ChargeProduct;
  quantity: number;
  isPrescription: boolean;
  directions: string | null;
  overrideReason: string | null;
}

export interface ChargeFormProps {
  /** The product picked from the price list. */
  product: ChargeProduct;
  /** A prescription: asks for label directions and the button reads "Prescribe". */
  isPrescription?: boolean;
  /** Allergies the product conflicts with, e.g. ["Penicillin"]. Any conflict needs a written reason to go ahead. */
  conflicts?: string[];
  /** The signed-in person may prescribe (a vet), so may override an allergy warning. Defaults to true. */
  canOverride?: boolean;
  onCancel?: () => void;
  onAdd?: (charge: ChargeInput) => void;
}

/**
 * Quantity (and for a prescription, label directions) for a product being charged on a consultation. An allergy
 * conflict shows a red banner and needs a reason from a vet before it can be added. Mirrors Shared/ChargeForm.razor.
 */
export function ChargeForm({ product, isPrescription = false, conflicts = [], canOverride = true, onCancel, onAdd }: ChargeFormProps) {
  const [qty, setQty] = useState(1);
  const [directions, setDirections] = useState("");
  const [reason, setReason] = useState("");
  const conflicted = conflicts.length > 0;
  const canAdd = qty > 0 && (!isPrescription || directions.trim() !== "") && (!conflicted || (canOverride && reason.trim() !== ""));

  return (
    <div className="vd-card" style={{ marginTop: 10, padding: 12, background: "var(--vd-surface-muted)" }}>
      <div className="vd-strong">{product.name}{product.isControlledDrug && <span className="vd-cd">CD</span>}</div>
      <div className="vd-cell-sub">{fmtMoney(product.grossPrice)} per {product.unit} incl. VAT{product.isStocked ? ` · ${fmtQty(product.onHand ?? 0)} in stock` : ""}</div>
      {conflicted && (
        <div className="vd-banner vd-banner-danger" style={{ marginTop: 10 }}>
          <Icon name="alert" size={18} />
          <span>This conflicts with the patient's allergy to <strong>{conflicts.join(", ")}</strong>.</span>
        </div>
      )}
      <div className="vd-field" style={{ maxWidth: 140 }}><label htmlFor="cf-qty">Quantity ({product.unit})</label><input id="cf-qty" type="number" min="0.01" step="any" value={qty} onChange={e => setQty(Number(e.target.value))} /></div>
      {isPrescription && (
        <div className="vd-field"><label htmlFor="cf-dir">Directions for the label</label><input id="cf-dir" value={directions} onChange={e => setDirections(e.target.value)} placeholder="e.g. 1 tablet twice daily with food" maxLength={400} /></div>
      )}
      {conflicted && (canOverride
        ? <div className="vd-field"><label htmlFor="cf-reason">Reason to go ahead</label><input id="cf-reason" value={reason} onChange={e => setReason(e.target.value)} placeholder="e.g. Tolerated previously; owner informed" maxLength={400} /></div>
        : <div className="vd-error-text">Only a vet can override an allergy warning.</div>)}
      <div className="vd-actions vd-actions-end" style={{ marginTop: 12 }}>
        <button type="button" className="vd-btn vd-btn-secondary vd-btn-sm" onClick={onCancel}>Cancel</button>
        <button type="button" className={cx("vd-btn", conflicted ? "vd-btn-danger" : "vd-btn-primary", "vd-btn-sm")} disabled={!canAdd}
          onClick={() => onAdd?.({ product, quantity: qty, isPrescription, directions: isPrescription ? directions : null, overrideReason: conflicted ? reason : null })}>
          {conflicted ? "Override and add" : isPrescription ? "Prescribe" : "Add"}
        </button>
      </div>
    </div>
  );
}
