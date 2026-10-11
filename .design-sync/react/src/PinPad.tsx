import { useEffect, useRef, useState } from "react";

export interface PinPadProps {
  /** The input's id. Defaults to "pin". */
  id?: string;
  /** Accessible label for the input. Defaults to "PIN". */
  label?: string;
  /** Disables the keypad while the PIN is checked. */
  busy?: boolean;
  /** Called with the four digits as soon as the fourth is entered; the pad then clears. */
  onComplete?: (pin: string) => void;
}

/**
 * Four-digit PIN entry: type it or tap the keypad (for touch screens). Used on the sign-in and lock screens.
 * Mirrors Shared/PinPad.razor.
 */
export function PinPad({ id = "pin", label = "PIN", busy = false, onComplete }: PinPadProps) {
  const [pin, setPin] = useState("");
  const input = useRef<HTMLInputElement>(null);
  useEffect(() => { input.current?.focus(); }, []);

  const set = (next: string) => {
    if (next.length < 4) { setPin(next); return; }
    setPin("");
    onComplete?.(next);
  };
  const press = (digit: string) => { if (pin.length < 4) set(pin + digit); };

  return (
    <div className="vd-pinpad">
      <input id={id} className="vd-input vd-pin-input" type="password" inputMode="numeric" autoComplete="off" maxLength={4}
        aria-label={label} value={pin} onChange={e => set(e.target.value.replace(/[^0-9]/g, "").slice(0, 4))} disabled={busy} ref={input} />
      <div className="vd-pin-keys">
        {[..."123456789"].map(key => (
          <button key={key} type="button" className="vd-pin-key" disabled={busy} onClick={() => press(key)}>{key}</button>
        ))}
        <button type="button" className="vd-pin-key vd-pin-key-small" disabled={busy} onClick={() => setPin("")}>Clear</button>
        <button type="button" className="vd-pin-key" disabled={busy} onClick={() => press("0")}>0</button>
        <button type="button" className="vd-pin-key vd-pin-key-small" disabled={busy} onClick={() => setPin(pin.slice(0, -1))} aria-label="Delete last digit">Delete</button>
      </div>
    </div>
  );
}
