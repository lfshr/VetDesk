import { FormError } from "@vetdesk/ui";

export const UnderAForm = () => (
  <div style={{ width: 480 }}>
    <div className="vd-form-grid">
      <div className="vd-field"><label htmlFor="fe-date">Date</label><input id="fe-date" defaultValue="12/10/2026" /></div>
      <div className="vd-field"><label htmlFor="fe-time">Time</label><input id="fe-time" defaultValue="09:20" /></div>
    </div>
    <FormError text="That time is already booked for Heather. Pick another time or another vet." />
  </div>
);

export const Short = () => (
  <div style={{ width: 480 }}>
    <FormError text="Enter the owner's phone number." />
  </div>
);
