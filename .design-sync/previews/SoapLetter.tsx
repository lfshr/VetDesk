import { SoapLetter } from "@vetdesk/ui";

export const Letters = () => (
  <div className="vd-row" style={{ gap: 10 }}>
    <SoapLetter letter="S" /><SoapLetter letter="O" /><SoapLetter letter="A" /><SoapLetter letter="P" />
  </div>
);

// A section of the consultation note (Pages/Consultation.razor).
export const NoteSection = () => (
  <section className="vd-card vd-card-body" style={{ width: 560 }}>
    <label htmlFor="sl-subj" className="vd-row" style={{ gap: 10, marginBottom: 8 }}>
      <SoapLetter letter="S" /><span className="vd-section-title" style={{ margin: 0 }}>Subjective</span><span className="vd-small vd-muted">History from owner</span>
    </label>
    <textarea id="sl-subj" className="vd-input" rows={3} defaultValue="Lame on left fore since a walk on Sunday. Eating and drinking normally. No vomiting." />
  </section>
);
