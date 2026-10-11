import { Check } from "@vetdesk/ui";

// The checks at the top of "Complete consultation and invoice".
export const ReadyToComplete = () => (
  <div style={{ width: 460 }}>
    <div className="vd-label">Checks</div>
    <ul className="vd-list">
      <Check ok text="Clinical note has all four sections filled in" />
      <Check ok text="No allergy conflicts" />
      <Check ok text="1 vaccine will be added to the vaccination record" />
      <Check ok text="2 items will go to the pharmacy queue to dispense" />
    </ul>
  </div>
);

export const Blocked = () => (
  <div style={{ width: 460 }}>
    <div className="vd-label">Checks</div>
    <ul className="vd-list">
      <Check ok={false} text="Fill in all four SOAP sections first" />
      <Check ok={false} text="Amoxicillin 250 mg tablets conflicts with the penicillin allergy" />
    </ul>
  </div>
);
