// Browser helpers for the VetDesk screens, loaded by VetDesk.Web.
window.vetdesk = (() => {
    let hotkeys = null;
    let lastActivity = Date.now();

    ["mousedown", "keydown", "wheel", "touchstart"].forEach(type =>
        document.addEventListener(type, () => { lastActivity = Date.now(); }, { passive: true, capture: true }));

    document.addEventListener("keydown", (e) => {
        // Ctrl+K (Cmd+K on a Mac) jumps to the patient search box, as on the design.
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k") {
            const box = document.getElementById("vd-patient-search");
            if (box) {
                e.preventDefault();
                box.focus();
                box.select();
                return;
            }
        }

        if (!hotkeys) return;
        const typing = e.target instanceof HTMLElement &&
            (e.target.isContentEditable || ["INPUT", "TEXTAREA", "SELECT"].includes(e.target.tagName));
        const combo = (e.ctrlKey || e.metaKey ? "Ctrl+" : "") + (e.altKey ? "Alt+" : "") + (e.shiftKey ? "Shift+" : "") + e.key;
        // Plain letters are ignored while typing; Ctrl/Alt combinations and Escape always count.
        if (typing && !(e.ctrlKey || e.metaKey || e.altKey) && e.key !== "Escape") return;
        const handled = ["Ctrl+s", "Ctrl+Enter", "Alt+n", "Alt+p", "Alt+c"].includes(combo);
        if (handled) e.preventDefault();
        hotkeys.invokeMethodAsync("OnHotkey", combo);
    });

    return {
        registerHotkeys: (ref) => { hotkeys = ref; },
        idleSeconds: () => Math.floor((Date.now() - lastActivity) / 1000),
        focus: (id) => { const el = document.getElementById(id); if (el) { el.focus(); if (el.select) el.select(); } },
        print: () => window.print(),
        // Prints on A4 with page numbers (the invoice). The page size is only set for this print so labels and other
        // printouts keep the printer's own paper. An optional footer line goes in the page's bottom margin, so it sits
        // at the foot of every page; a position: fixed footer would overlap the content instead.
        printA4: (footer) => {
            const box = "font: 7pt Consolas, monospace; color: #7A8683; vertical-align: top; padding-top: 4mm;"
                + (footer ? " border-top: .25mm solid #DDE3E1;" : "");
            const footerBox = footer
                ? `@bottom-left { content: "${footer.replace(/\\/g, "\\\\").replace(/"/g, '\\"').replace(/\s+/g, " ")}"; ${box} width: 155mm; font-family: "IBM Plex Sans", "Segoe UI", sans-serif; }`
                : "";
            const style = document.createElement("style");
            style.textContent = `@page { size: A4; margin: 14mm 16mm 18mm; ${footerBox} @bottom-right { content: 'Page ' counter(page) ' of ' counter(pages); ${box} } }`;
            document.head.appendChild(style);
            document.documentElement.classList.add("vd-print-a4");
            window.addEventListener("afterprint", () => {
                style.remove();
                document.documentElement.classList.remove("vd-print-a4");
            }, { once: true });
            window.print();
        },
        scrollIntoView: (id) => document.getElementById(id)?.scrollIntoView({ block: "center" }),
        // Saves text as a file (CSV exports). In the desktop app WebView2 shows its own download prompt.
        download: (fileName, mimeType, text) => {
            const blob = new Blob(["﻿" + text], { type: mimeType });
            const url = URL.createObjectURL(blob);
            const a = document.createElement("a");
            a.href = url;
            a.download = fileName;
            document.body.appendChild(a);
            a.click();
            a.remove();
            setTimeout(() => URL.revokeObjectURL(url), 1000);
        },
    };
})();
