import { cx } from "./format.js";

export type InvoiceStatus = "Unpaid" | "Overdue" | "PartPaid" | "ClaimSent" | "Paid" | "Refunded";

export interface InvoiceStatusChipProps {
  /** The invoice's payment status. */
  status: InvoiceStatus;
}

const css: Record<InvoiceStatus, string> = {
  Paid: "vd-chip-success",
  Overdue: "vd-chip-danger",
  PartPaid: "vd-chip-warning",
  ClaimSent: "vd-chip-info",
  Unpaid: "vd-chip-warning",
  Refunded: "",
};
const label: Partial<Record<InvoiceStatus, string>> = { PartPaid: "Part paid", ClaimSent: "Claim sent" };

/** Payment status pill on the Billing screen and invoices. Mirrors Shared/InvoiceStatusChip.razor. */
export function InvoiceStatusChip({ status }: InvoiceStatusChipProps) {
  return <span className={cx("vd-chip", css[status])}>{label[status] ?? status}</span>;
}
