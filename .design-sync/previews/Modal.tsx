import { Modal, PatientPicker } from "@vetdesk/ui";

// The overlay is position:fixed. In the app it covers the screen; here the transform makes this stage its
// containing block, so the open dialog renders inside the card.
const Stage = ({ height = 600, children }: { height?: number; children: React.ReactNode }) => (
  <div style={{ position: "relative", transform: "translateZ(0)", width: 760, height, overflow: "hidden" }}>{children}</div>
);

const footer = (primary: string) => (
  <>
    <button type="button" className="vd-btn vd-btn-secondary">Cancel</button>
    <button type="button" className="vd-btn vd-btn-primary">{primary}</button>
  </>
);

// The shape of Dialogs/AppointmentDialog.razor with a patient already chosen.
export const NewAppointment = () => (
  <Stage height={560}><Modal title="New appointment" subtitle="Monday 12 October 2026" footer={footer("Book appointment")}>
    <PatientPicker selected={{ id: 1, name: "Biscuit", species: "Dog", speciesAndBreed: "Dog · Border Terrier", ownerName: "Sarah Whitfield", allergyLabels: ["Penicillin"] }} />
    <div className="vd-form-grid" style={{ marginTop: 12 }}>
      <div className="vd-field"><label htmlFor="na-date">Date</label><input id="na-date" type="date" defaultValue="2026-10-12" /></div>
      <div className="vd-field"><label htmlFor="na-time">Time</label><input id="na-time" defaultValue="09:20" /></div>
      <div className="vd-field">
        <label htmlFor="na-type">Type</label>
        <select id="na-type" defaultValue="Consultation"><option>Consultation</option><option>Vaccination</option><option>Surgery</option></select>
      </div>
      <div className="vd-field">
        <label htmlFor="na-with">With</label>
        <select id="na-with" defaultValue="Heather"><option>Heather</option><option>Douglas</option><option>Jen</option></select>
      </div>
      <div className="vd-field vd-field-wide"><label htmlFor="na-reason">Reason</label><input id="na-reason" defaultValue="Limping, left fore" /></div>
    </div>
  </Modal></Stage>
);

export const Narrow = () => (
  <Stage height={400}><Modal title="Cash-up" subtitle="Saturday 10 October" size="narrow" footer={footer("Save cash-up")}>
    <div className="vd-field" style={{ marginTop: 0 }}><label htmlFor="cu-cash">Cash counted</label><input id="cu-cash" className="vd-mono" defaultValue="£312.40" /></div>
    <div className="vd-field"><label htmlFor="cu-card">Card terminal total</label><input id="cu-card" className="vd-mono" defaultValue="£1,846.75" /></div>
  </Modal></Stage>
);

export const WideFlushTable = () => (
  <Stage height={360}><Modal title="Controlled drug register" subtitle="Methadone 10 mg/ml injection" size="wide" flush>
    <table className="vd-table">
      <thead><tr><th>Date</th><th>Entry</th><th>By</th><th>Witness</th><th className="vd-num">Balance</th></tr></thead>
      <tbody>
        <tr><td>8 Oct 2026</td><td>Administered to Biscuit, 0.4 ml</td><td>Heather</td><td>Janet</td><td className="vd-num vd-mono">18.0 ml</td></tr>
        <tr><td>2 Oct 2026</td><td>Received from supplier, 20 ml</td><td>Lorna</td><td>Douglas</td><td className="vd-num vd-mono">18.4 ml</td></tr>
      </tbody>
    </table>
  </Modal></Stage>
);
