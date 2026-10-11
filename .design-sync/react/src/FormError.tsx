import { Icon } from "./Icon.js";

export interface FormErrorProps {
  /** Why the request was refused. Nothing renders without it. */
  text?: string | null;
}

/** The red banner under a form when a request is refused. Mirrors Shared/FormError.razor. */
export function FormError({ text }: FormErrorProps) {
  if (text == null) return null;
  return <div className="vd-banner vd-banner-danger" style={{ marginTop: 12 }} role="alert"><Icon name="warning" size={16} /> {text}</div>;
}
