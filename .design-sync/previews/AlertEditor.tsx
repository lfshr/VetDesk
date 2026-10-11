import { AlertEditor } from "@vetdesk/ui";

export const NewAlert = () => (
  <div style={{ width: 540 }}><AlertEditor alert={{ text: "", severity: "Alert" }} /></div>
);

export const EditPreset = () => (
  <div style={{ width: 540 }}><AlertEditor alert={{ id: 7, presetKey: "1", text: "Bites when restrained", severity: "Alert" }} /></div>
);

export const EditCustomNote = () => (
  <div style={{ width: 540 }}><AlertEditor alert={{ id: 9, presetKey: "custom", text: "Owner prefers text messages, not calls", severity: "Note" }} /></div>
);
