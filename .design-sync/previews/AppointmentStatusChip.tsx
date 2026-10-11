import { AppointmentStatusChip } from "@vetdesk/ui";

export const Consultations = () => (
  <div className="vd-row vd-wrap">
    <AppointmentStatusChip status="Booked" />
    <AppointmentStatusChip status="CheckedIn" />
    <AppointmentStatusChip status="InConsult" />
    <AppointmentStatusChip status="Done" />
    <AppointmentStatusChip status="Cancelled" />
    <AppointmentStatusChip status="NoShow" />
  </div>
);

export const Surgery = () => (
  <div className="vd-row vd-wrap">
    <AppointmentStatusChip status="Booked" kind="Surgery" />
    <AppointmentStatusChip status="CheckedIn" kind="Surgery" />
    <AppointmentStatusChip status="InConsult" kind="Surgery" />
    <AppointmentStatusChip status="Done" kind="Surgery" />
  </div>
);

// Ported from the appointments table on the Today screen (Pages/Today.razor).
export const InTodaysAppointments = () => (
  <section className="vd-card" style={{ width: 720 }}>
    <table className="vd-table" aria-label="Today's appointments">
      <thead><tr><th>Time</th><th>Patient</th><th>Owner</th><th>Reason</th><th>Status</th></tr></thead>
      <tbody>
        <tr className="vd-clickable">
          <td className="vd-mono">09:00</td>
          <td><div className="vd-cell-primary">Rolo</div><div className="vd-cell-sub">Dog · Labrador Retriever</div></td>
          <td>Gareth</td><td>Booster vaccination</td>
          <td><AppointmentStatusChip status="Done" /></td>
        </tr>
        <tr className="vd-clickable selected">
          <td className="vd-mono">09:20</td>
          <td><div className="vd-cell-primary">Biscuit <span className="vd-chip vd-chip-danger" style={{ height: 18, padding: "0 6px", fontSize: 11 }}>Allergy</span></div><div className="vd-cell-sub">Dog · Border Terrier</div></td>
          <td>Sarah</td><td>Limping, left fore</td>
          <td><AppointmentStatusChip status="InConsult" /></td>
        </tr>
        <tr className="vd-clickable">
          <td className="vd-mono">09:40</td>
          <td><div className="vd-cell-primary">Mittens</div><div className="vd-cell-sub">Cat · Maine Coon</div></td>
          <td>Helen</td><td>Weight check</td>
          <td><AppointmentStatusChip status="CheckedIn" /></td>
        </tr>
        <tr className="vd-clickable">
          <td className="vd-mono">10:30</td>
          <td><div className="vd-cell-primary">Bramble</div><div className="vd-cell-sub">Cat · Domestic Shorthair</div></td>
          <td>Tom</td><td>Dental, scale and polish</td>
          <td><AppointmentStatusChip status="Booked" kind="Surgery" /></td>
        </tr>
        <tr className="vd-clickable">
          <td className="vd-mono">11:00</td>
          <td><div className="vd-cell-primary">Pickle</div><div className="vd-cell-sub">Guinea pig</div></td>
          <td>Priya</td><td>Overgrown teeth</td>
          <td><AppointmentStatusChip status="Booked" /></td>
        </tr>
      </tbody>
    </table>
  </section>
);
