import { Kpi } from "@vetdesk/ui";

// The KPI row at the top of the Reports screen.
export const ReportsRow = () => (
  <div className="vd-kpis" style={{ width: 860 }}>
    <Kpi label="Revenue (net)" value="£18,240.50" change={6.2} />
    <Kpi label="Consultations" value="212" change={-3.1} />
    <Kpi label="New clients" value="14" change={0} />
    <Kpi label="Average invoice" value="£86.40" />
  </div>
);

export const Single = () => (
  <div style={{ width: 220 }}>
    <Kpi label="Takings today" value="£2,159.15" change={12.4} />
  </div>
);
