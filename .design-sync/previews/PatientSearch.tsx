import { PatientSearch } from "@vetdesk/ui";

const patients = [
  { id: 1, name: "Biscuit", patientNumber: "P-00412", species: "Dog", speciesAndBreed: "Dog · Border Terrier", ownerName: "Sarah Whitfield", ownerPhone: "07700 900461" },
  { id: 2, name: "Bilbo", patientNumber: "P-01027", species: "Rabbit", speciesAndBreed: "Rabbit · Netherland Dwarf", ownerName: "Amira Khan", ownerPhone: "07700 900188" },
  { id: 3, name: "Bella", patientNumber: "P-00733", species: "Cat", speciesAndBreed: "Cat · Ragdoll", ownerName: "Tom Bishop", ownerPhone: "0113 496 0721" },
  { id: 4, name: "Mittens", patientNumber: "P-00211", species: "Cat", speciesAndBreed: "Cat · Maine Coon", ownerName: "Helen Ashby", ownerPhone: "07700 900733" },
];

// The dropdown is absolutely positioned; the frame gives it room inside the card.
const frame = (children: React.ReactNode) => <div style={{ width: 560, minHeight: 330 }}>{children}</div>;

export const Empty = () => frame(<PatientSearch patients={patients} />);

export const Matches = () => frame(<PatientSearch patients={patients} defaultQuery="bi" />);

export const NoMatch = () => frame(<PatientSearch patients={patients} defaultQuery="Zebedee" />);
