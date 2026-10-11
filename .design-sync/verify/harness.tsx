// Mounts one React mirror into #case for compare.mjs. Props written as {"$html": "..."} become React elements, so a
// Modal case can pass the same body markup to both sides.
import { createElement, type ReactNode } from "react";
import { createRoot } from "react-dom/client";
import * as mirrors from "../react/dist/index.js";

function toReact(node: Node, key: number): ReactNode {
  if (node.nodeType === Node.TEXT_NODE) return node.textContent;
  if (node.nodeType !== Node.ELEMENT_NODE) return null;
  const el = node as Element;
  const props: Record<string, unknown> = { key };
  for (const a of el.attributes) {
    if (a.name === "class") props.className = a.value;
    else if (a.name === "for") props.htmlFor = a.value;
    else if (a.name === "style") props.style = Object.fromEntries(a.value.split(";").filter(Boolean).map(d => {
      const [k, ...v] = d.split(":");
      return [k.trim().replace(/-([a-z])/g, (_, c) => c.toUpperCase()), v.join(":").trim()];
    }));
    else props[a.name] = a.value;
  }
  return createElement(el.localName, props, ...[...el.childNodes].map(toReact));
}

function revive(value: unknown): unknown {
  if (Array.isArray(value)) return value.map(revive);
  if (value && typeof value === "object") {
    if ("$html" in value) {
      const doc = new DOMParser().parseFromString(`<body>${(value as { $html: string }).$html}</body>`, "text/html");
      return [...doc.body.childNodes].map(toReact);
    }
    return Object.fromEntries(Object.entries(value).map(([k, v]) => [k, revive(v)]));
  }
  return value;
}

(window as unknown as { mount: (name: string, props: object) => void }).mount = (name, props) => {
  const component = (mirrors as Record<string, React.ComponentType<object>>)[name];
  createRoot(document.getElementById("case")!).render(createElement(component, revive(props) as object));
};
