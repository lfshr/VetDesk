import { Toasts } from "@vetdesk/ui";

// Toasts are fixed to the bottom-right corner; the transform makes this stage their containing block.
const Stage = ({ children }: { children: React.ReactNode }) => (
  <div style={{ position: "relative", transform: "translateZ(0)", width: 520, height: 220, overflow: "hidden", background: "var(--vd-canvas)" }}>{children}</div>
);

export const Kinds = () => (
  <Stage>
    <Toasts toasts={[
      { id: "1", text: "Biscuit checked in" },
      { id: "2", text: "Low stock: Amoxicillin 250 mg tablets", kind: "Warning" },
      { id: "3", text: "VetDesk couldn't save the invoice. Try again.", kind: "Error" },
    ]} />
  </Stage>
);

export const One = () => (
  <Stage><Toasts toasts={[{ id: "1", text: "Invoice INV-2041 paid: £86.40 by card" }]} /></Stage>
);
