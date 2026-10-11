import { ConfirmDialog } from "@vetdesk/ui";

// The overlay is position:fixed; the transform makes this stage its containing block so it renders in the card.
const Stage = ({ children }: { children: React.ReactNode }) => (
  <div style={{ position: "relative", transform: "translateZ(0)", width: 640, height: 300, overflow: "hidden" }}>{children}</div>
);

export const Delete = () => (
  <Stage><ConfirmDialog title="Are you sure you want to delete “Bites when restrained”?" confirmLabel="Delete" danger /></Stage>
);

export const Question = () => (
  <Stage><ConfirmDialog title="Check Biscuit in?" message="Sarah Whitfield is at the desk. Biscuit is booked with Heather at 09:20." confirmLabel="Check in" cancelLabel="Not yet" /></Stage>
);

export const Alert = () => (
  <Stage><ConfirmDialog title="This consultation is locked" message="It was completed by Heather on 8 Oct 2026. Add a follow-up note instead." cancelLabel={null} /></Stage>
);
