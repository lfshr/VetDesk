import { Vital } from "@vetdesk/ui";

// The vitals row in the Objective section of the consultation note.
export const VitalsRow = () => (
  <div className="vd-form-grid-3" style={{ gridTemplateColumns: "repeat(6, minmax(0, 1fr))", width: 820 }}>
    <Vital label="Temperature" unit="°C" id="v-temp" value="38.6" />
    <Vital label="Heart rate" unit="bpm" id="v-hr" value="96" />
    <Vital label="Resp. rate" unit="/min" id="v-rr" value="24" />
    <Vital label="Weight" unit="kg" id="v-wt" value="12.4" />
    <Vital label="Mucous membr." id="v-mm" value="Pink, moist" text />
    <Vital label="CRT" unit="sec" id="v-crt" value="<2" text />
  </div>
);

export const Locked = () => (
  <div className="vd-form-grid-3" style={{ width: 420 }}>
    <Vital label="Temperature" unit="°C" id="vl-temp" value="38.9" disabled />
    <Vital label="Heart rate" unit="bpm" id="vl-hr" value="110" disabled />
    <Vital label="Weight" unit="kg" id="vl-wt" value="4.2" disabled />
  </div>
);

export const Empty = () => (
  <div style={{ width: 140 }}>
    <Vital label="Temperature" unit="°C" id="ve-temp" />
  </div>
);
