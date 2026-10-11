// Copies the app's real stylesheet into dist/ so the Claude Design bundle ships it unchanged.
// vetdesk.css stays the source of truth; never edit the copy.
import { copyFileSync } from "node:fs";
copyFileSync(new URL("../../src/VetDesk.UI/wwwroot/vetdesk.css", import.meta.url), new URL("./dist/vetdesk.css", import.meta.url));
