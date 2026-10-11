import { cx } from "./format.js";

export type AppointmentStatus = "Booked" | "CheckedIn" | "InConsult" | "Done" | "Cancelled" | "NoShow";
export type AppointmentKind = "Consultation" | "Surgery";

export interface AppointmentStatusChipProps {
  /** Where the appointment is in the day. */
  status: AppointmentStatus;
  /** Consultation (default) or Surgery: surgery reads "Surgery", "Admitted" and "In theatre" instead. */
  kind?: AppointmentKind;
}

/** Status pill for an appointment on the Today and Schedule screens. Mirrors Shared/AppointmentStatusChip.razor. */
export function AppointmentStatusChip({ status, kind = "Consultation" }: AppointmentStatusChipProps) {
  const surgery = kind === "Surgery";
  const css =
    status === "Done" ? "" :
    status === "InConsult" ? "vd-chip-info" :
    status === "CheckedIn" ? "vd-chip-warning" :
    status === "Cancelled" || status === "NoShow" ? "vd-chip-danger" :
    surgery ? "vd-chip-surgery" :
    // Booked and not yet arrived: outlined, so the ones that need attention (waiting, in consult) stand out.
    "vd-chip-outline";
  const label =
    status === "InConsult" ? (surgery ? "In theatre" : "In consult") :
    status === "CheckedIn" ? (surgery ? "Admitted" : "Waiting") :
    status === "NoShow" ? "No-show" :
    status === "Booked" && surgery ? "Surgery" :
    status;
  return <span className={cx("vd-chip", css)}>{label}</span>;
}
