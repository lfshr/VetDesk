// Browser helpers for the VetDesk screens. Loaded by both hosts.
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
