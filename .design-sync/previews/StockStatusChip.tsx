import { StockStatusChip } from "@vetdesk/ui";

export const AllStatuses = () => (
  <div className="vd-row vd-wrap">
    <StockStatusChip status="InStock" />
    <StockStatusChip status="Low" />
    <StockStatusChip status="Out" />
    <StockStatusChip status="Expiring" />
    <StockStatusChip status="Expired" />
  </div>
);

// The stock table on the Pharmacy screen.
export const InStockList = () => (
  <section className="vd-card" style={{ width: 640 }}>
    <table className="vd-table">
      <thead><tr><th>Product</th><th>Location</th><th className="vd-num">On hand</th><th>Status</th></tr></thead>
      <tbody>
        <tr><td><div className="vd-cell-primary">Meloxicam 1.5 mg/ml oral suspension</div><div className="vd-cell-sub vd-mono">MEL15</div></td><td>Shelf B2</td><td className="vd-num vd-mono">23</td><td><StockStatusChip status="InStock" /></td></tr>
        <tr><td><div className="vd-cell-primary">Amoxicillin 250 mg tablets</div><div className="vd-cell-sub vd-mono">AMX250</div></td><td>Shelf A1</td><td className="vd-num vd-mono">40</td><td><StockStatusChip status="Low" /></td></tr>
        <tr><td><div className="vd-cell-primary">Methadone 10 mg/ml injection <span className="vd-cd">CD</span></div><div className="vd-cell-sub vd-mono">METH10</div></td><td>CD cabinet</td><td className="vd-num vd-mono">18</td><td><StockStatusChip status="Expiring" /></td></tr>
        <tr><td><div className="vd-cell-primary">Cefovecin 80 mg/ml injection</div><div className="vd-cell-sub vd-mono">CEF80</div></td><td>Fridge</td><td className="vd-num vd-mono">0</td><td><StockStatusChip status="Out" /></td></tr>
      </tbody>
    </table>
  </section>
);
