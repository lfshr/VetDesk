// Formatting as the app's Services/Fmt.cs does it: pounds, UK dates in the clinic's time zone.
const money = new Intl.NumberFormat("en-GB", { style: "currency", currency: "GBP" });
const date = new Intl.DateTimeFormat("en-GB", { day: "numeric", month: "short", year: "numeric", timeZone: "Europe/London" });
const dateOnly = new Intl.DateTimeFormat("en-GB", { day: "numeric", month: "short", year: "numeric", timeZone: "UTC" });

/** Fmt.Money: "£1,234.50". */
export const fmtMoney = (amount: number) => money.format(amount);

/** Fmt.Qty: up to three decimals, no trailing zeros. */
export const fmtQty = (quantity: number) => String(Math.round(quantity * 1000) / 1000);

/** Fmt.Date for a UTC timestamp: "3 Nov 2026", in UK local time. */
export const fmtDate = (utc: string | Date) => date.format(new Date(utc)).replace(/,/g, "");

/** Fmt.Date for a calendar date ("2026-11-03"): "3 Nov 2026". */
export const fmtDateOnly = (isoDate: string) => dateOnly.format(new Date(`${isoDate.slice(0, 10)}T00:00:00Z`)).replace(/,/g, "");

/** Joins class names, skipping empty ones. */
export const cx = (...names: (string | false | null | undefined)[]) => names.filter(Boolean).join(" ");
