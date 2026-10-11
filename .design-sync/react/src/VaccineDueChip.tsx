import { cx, fmtDateOnly } from "./format.js";

export interface VaccineDueChipProps {
  /** When the booster is due, as an ISO date ("2026-11-03"). Leave out when no booster is needed. */
  dueDate?: string | null;
  /** The due date has passed: amber "Overdue since …". */
  isOverdue?: boolean;
  /** Due within the reminder window: blue. */
  isDueSoon?: boolean;
}

/** Booster due chip on a patient's vaccinations. Mirrors Shared/VaccineDueChip.razor. */
export function VaccineDueChip({ dueDate, isOverdue = false, isDueSoon = false }: VaccineDueChipProps) {
  const css = isOverdue ? "vd-chip-warning" : isDueSoon ? "vd-chip-info" : dueDate == null ? "" : "vd-chip-success";
  const text = dueDate == null ? "No booster" : isOverdue ? `Overdue since ${fmtDateOnly(dueDate)}` : `Due ${fmtDateOnly(dueDate)}`;
  return <span className={cx("vd-chip", css)}>{text}</span>;
}
