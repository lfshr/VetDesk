import { PinPad } from "@vetdesk/ui";

// The PIN step of the sign-in screen, after tapping a name tile.
export const SignIn = () => (
  <div className="vd-card vd-card-body" style={{ width: 300 }}>
    <div className="vd-row" style={{ gap: 10, marginBottom: 12 }}>
      <span className="vd-avatar">J</span>
      <div><div className="vd-strong">Janet</div><div className="vd-small vd-muted">Registered veterinary nurse</div></div>
    </div>
    <PinPad label="Janet's PIN" />
  </div>
);

export const Busy = () => (
  <div className="vd-card vd-card-body" style={{ width: 300 }}>
    <PinPad id="pin-busy" busy />
  </div>
);
