import { Icon } from "./Icon.js";
import { cx } from "./format.js";

export type AlertSeverity = "Alert" | "Warning" | "Note";

export interface AlertChipProps {
  /** The alert text, e.g. "Bites when restrained" or an allergy such as "Penicillin". */
  text: string;
  /** How serious it is: Alert (red), Warning (amber) or Note (green). Defaults to Alert. */
  severity?: AlertSeverity;
  /** A drug allergy: always red, titled "Drug allergy". */
  isAllergy?: boolean;
  /** The bigger, outlined chip the patient record header uses. */
  large?: boolean;
}

/** A patient alert or allergy chip, shown under the patient's name. Mirrors Shared/AlertChip.razor. */
export function AlertChip({ text, severity = "Alert", isAllergy = false, large = false }: AlertChipProps) {
  const css = isAllergy || severity === "Alert" ? "vd-chip-danger" : severity === "Warning" ? "vd-chip-warning" : "vd-chip-success";
  return (
    <span className={cx("vd-chip", css, large && "vd-chip-lg")} title={isAllergy ? "Drug allergy" : severity}>
      <Icon name={severity === "Note" ? "info" : "alert"} size={13} /> {text}
    </span>
  );
}
