export interface SoapLetterProps {
  /** The section letter: S, O, A or P. */
  letter: string;
}

/** The teal letter badge beside each SOAP section of a consultation note. Mirrors Shared/SoapLetter.razor. */
export function SoapLetter({ letter }: SoapLetterProps) {
  return (
    <span aria-hidden="true" style={{ display: "inline-grid", placeItems: "center", width: 26, height: 26, borderRadius: 6, background: "var(--vd-accent-wash)", color: "var(--vd-accent)", fontWeight: 700, fontSize: 13 }}>
      {letter}
    </span>
  );
}
