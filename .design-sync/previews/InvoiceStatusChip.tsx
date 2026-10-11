import { InvoiceStatusChip } from "@vetdesk/ui";

export const AllStatuses = () => (
  <div className="vd-row vd-wrap">
    <InvoiceStatusChip status="Unpaid" />
    <InvoiceStatusChip status="PartPaid" />
    <InvoiceStatusChip status="Overdue" />
    <InvoiceStatusChip status="ClaimSent" />
    <InvoiceStatusChip status="Paid" />
    <InvoiceStatusChip status="Refunded" />
  </div>
);

// The invoices table on the Billing screen.
export const InInvoices = () => (
  <section className="vd-card" style={{ width: 640 }}>
    <table className="vd-table">
      <thead><tr><th>Invoice</th><th>Client</th><th>Date</th><th className="vd-num">Total</th><th>Status</th></tr></thead>
      <tbody>
        <tr className="vd-clickable"><td className="vd-mono">INV-2041</td><td>Sarah Whitfield</td><td>10 Oct 2026</td><td className="vd-num vd-mono">£86.40</td><td><InvoiceStatusChip status="Paid" /></td></tr>
        <tr className="vd-clickable"><td className="vd-mono">INV-2040</td><td>Tom Bishop</td><td>10 Oct 2026</td><td className="vd-num vd-mono">£312.00</td><td><InvoiceStatusChip status="ClaimSent" /></td></tr>
        <tr className="vd-clickable"><td className="vd-mono">INV-2033</td><td>Helen Ashby</td><td>3 Oct 2026</td><td className="vd-num vd-mono">£54.20</td><td><InvoiceStatusChip status="PartPaid" /></td></tr>
        <tr className="vd-clickable"><td className="vd-mono">INV-1987</td><td>Gareth Price</td><td>12 Sept 2026</td><td className="vd-num vd-mono">£128.75</td><td><InvoiceStatusChip status="Overdue" /></td></tr>
      </tbody>
    </table>
  </section>
);
