import { cx } from "./format.js";

export type StockStatus = "InStock" | "Low" | "Out" | "Expiring" | "Expired";

export interface StockStatusChipProps {
  /** Stock level or expiry state of a product. Defaults to InStock. */
  status?: StockStatus;
}

/** Stock pill on the Pharmacy screen. Mirrors Shared/StockStatusChip.razor. */
export function StockStatusChip({ status = "InStock" }: StockStatusChipProps) {
  const css = status === "Out" || status === "Expired" ? "vd-chip-danger" : status === "Low" || status === "Expiring" ? "vd-chip-warning" : "vd-chip-success";
  const label = status === "InStock" ? "In stock" : status === "Out" ? "Out of stock" : status === "Expiring" ? "Expiring soon" : status;
  return <span className={cx("vd-chip", css)}>{label}</span>;
}
