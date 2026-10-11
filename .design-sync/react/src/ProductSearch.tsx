import { useState } from "react";
import { Icon } from "./Icon.js";
import { cx, fmtMoney, fmtQty } from "./format.js";
import { matchProducts } from "./search.js";
import type { ProductCategory, ProductRow } from "./types.js";

export interface ProductSearchProps {
  /** The price list to search (up to twelve matches show, in this order). Give each product the current patient's `allergyConflicts`. */
  products: ProductRow[];
  /** Only these categories, e.g. ["Medicine"] when prescribing. */
  categories?: ProductCategory[];
  /** Only products kept in stock. */
  stockedOnly?: boolean;
  /** Defaults to "Search the price list". */
  placeholder?: string;
  /** Defaults to "product-search". */
  inputId?: string;
  /** Picking a product (click, or Enter for the highlighted one). The box then clears. */
  onPick?: (product: ProductRow) => void;
  /** Text already in the box, with its dropdown open (for a static mockup of the search in use). */
  defaultQuery?: string;
}

/**
 * Searches the price list when charging or prescribing. With a patient, each match shows any conflict with that
 * patient's allergies. Mirrors Shared/ProductSearch.razor.
 */
export function ProductSearch({ products, categories, stockedOnly = false, placeholder = "Search the price list", inputId = "product-search", onPick, defaultQuery = "" }: ProductSearchProps) {
  const [query, setQuery] = useState(defaultQuery);
  const [open, setOpen] = useState(defaultQuery !== "");
  const [active, setActive] = useState(-1);
  const text = query.trim();
  const results = text.length >= 2
    ? matchProducts(products.filter(p => !categories || categories.includes(p.category)), text).slice(0, 12).filter(p => !stockedOnly || p.isStocked)
    : [];

  const pick = (p: ProductRow) => { setOpen(false); setQuery(""); onPick?.(p); };
  const onKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "ArrowDown" && results.length > 0) setActive((active + 1) % results.length);
    else if (e.key === "ArrowUp" && results.length > 0) setActive(active <= 0 ? results.length - 1 : active - 1);
    else if (e.key === "Enter" && results.length > 0) pick(results[Math.max(0, active)]);
    else if (e.key === "Escape") setOpen(false);
  };

  return (
    <div style={{ position: "relative" }}>
      <div className="vd-search-box" style={{ maxWidth: "none" }}>
        <Icon name="search" size={18} />
        <input id={inputId} type="search" autoComplete="off" placeholder={placeholder} aria-label={placeholder} value={query}
          onChange={e => { setQuery(e.target.value); setOpen(true); setActive(-1); }} onKeyDown={onKeyDown}
          onBlur={() => setTimeout(() => setOpen(false), 150)} style={{ paddingRight: 12 }} />
      </div>
      {open && text.length >= 2 && (
        <div className="vd-dropdown" role="listbox" style={{ maxHeight: 320, overflowY: "auto" }}>
          {results.map((p, i) => (
            <button key={p.id} type="button" className={cx("vd-dropdown-item", i === active && "active")} role="option" aria-selected={i === active ? "true" : "false"} onMouseDown={() => pick(p)}>
              <span style={{ flex: 1, minWidth: 0 }}>
                <span className="vd-strong">{p.name}</span>
                {p.isControlledDrug && <span className="vd-cd">CD</span>}
                <span className="vd-cell-sub" style={{ display: "block" }}>
                  <span className="vd-mono">{p.code}</span> · {p.category}{p.isStocked ? ` · ${fmtQty(p.onHand ?? 0)} ${p.unit} in stock` : ""}
                </span>
                {(p.allergyConflicts?.length ?? 0) > 0 && (
                  <span className="vd-chip vd-chip-danger" style={{ marginTop: 4 }}><Icon name="alert" size={13} /> Conflicts with the {p.allergyConflicts!.join(", ")} allergy</span>
                )}
              </span>
              <span className="vd-mono vd-small">{fmtMoney(p.grossPrice)}</span>
            </button>
          ))}
          {results.length === 0 && <div className="vd-dropdown-item vd-muted">Nothing on the price list matches.</div>}
        </div>
      )}
    </div>
  );
}
