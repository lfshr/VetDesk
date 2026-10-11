import { PatientPicker } from "@vetdesk/ui";

const patients = [
  { id: 1, name: "Biscuit", species: "Dog", speciesAndBreed: "Dog · Border Terrier", ownerName: "Sarah Whitfield", ownerPhone: "07700 900461", allergyLabels: ["Penicillin"] },
  { id: 2, name: "Bilbo", species: "Rabbit", speciesAndBreed: "Rabbit · Netherland Dwarf", ownerName: "Amira Khan", ownerPhone: "07700 900188" },
  { id: 3, name: "Bella", species: "Cat", speciesAndBreed: "Cat · Ragdoll", ownerName: "Tom Bishop", ownerPhone: "0113 496 0721" },
];
const recent = [
  { id: 4, name: "Mittens", species: "Cat", speciesAndBreed: "Cat · Maine Coon", ownerName: "Helen Ashby" },
  { id: 5, name: "Rolo", species: "Dog", speciesAndBreed: "Dog · Labrador Retriever", ownerName: "Gareth Price" },
  { id: 6, name: "Pickle", species: "Guinea pig", speciesAndBreed: "Guinea pig", ownerName: "Priya Nair" },
];

const frame = (children: React.ReactNode) => <div style={{ width: 560 }}>{children}</div>;

export const Recent = () => frame(<PatientPicker patients={patients} recent={recent} />);

export const Matches = () => frame(<PatientPicker patients={patients} recent={recent} defaultQuery="bi" />);

export const NewClient = () => frame(<PatientPicker patients={patients} recent={recent} defaultNewOpen />);

export const Selected = () => frame(<PatientPicker selected={patients[0]} />);
