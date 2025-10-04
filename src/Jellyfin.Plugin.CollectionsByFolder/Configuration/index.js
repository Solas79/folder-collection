// Robuste Version: direktes Binden auf die konkreten Buttons + Key-Fallback.
// Keine externen Abhängigkeiten nötig.
define([], function () {
  "use strict";

  const pluginId = "f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41";
  const Api = (typeof ApiClient !== "undefined") ? ApiClient : null;
  const UI  = (typeof Dashboard !== "undefined") ? Dashboard : null;

  function showStatus(view, msg) {
    const el = view.querySelector("#cbf-status");
    if (!el) return;
    el.textContent = msg;
    clearTimeout(el._t);
    el._t = setTimeout(() => (el.textContent = ""), 5000);
  }

  function linesFrom(view, id) {
    const el = view.querySelector("#" + id);
    return (el?.value || "").split("\n").map(s => s.trim()).filter(Boolean);
  }
  function setLines(view, id, arr) {
    const el = view.querySelector("#" + id);
    if (el) el.value = (arr || []).join("\n");
  }

  async function loadConfig(view) {
    if (!Api) return;
    try {
      const cfg = await Api.getPluginConfiguration(pluginId);
      const wl = (cfg.Whitelist && cfg.Whitelist.length) ? cfg.Whitelist : (cfg.FolderPaths || []);
      setLines(view, "whitelist", wl);
      setLines(view, "blacklist", cfg.Blacklist || []);
      const g = (sel, v) => { const el = view.querySelector(sel); if (el) el.value = v; };
      g("#prefix",  cfg.Prefix   || "");
      g("#suffix",  cfg.Suffix   || "");
      g("#minItems", String(cfg.MinItemCount || 1));
      const chk = view.querySelector("#enableDailyScan");
      if (chk) chk.checked = !!cfg.EnableDailyScan;
      g("#scanTime", cfg.ScanTime || "03:00");
    } catch (e) {
      console.error("[CBF] loadConfig", e);
      UI && UI.alert("Konfiguration konnte nicht geladen werden.");
    }
  }

  async function saveConfig(view) {
    if (!Api) return;
    try {
      const cfg = await Api.getPluginConfiguration(pluginId);
      cfg.Whitelist = linesFrom(view, "whitelist");
      if (cfg.Whitelist?.length) cfg.FolderPaths = []; // alten Key neutralisieren
      cfg.Blacklist = linesFrom(view, "blacklist");
      const get = (sel) => (view.querySelector(sel)?.value || "").trim();
      cfg.Prefix = get("#prefix");
      cfg.Suffix = get("#suffix");
      cfg.MinItemCount = parseInt(get("#minItems") || "1", 10);
      cfg.EnableDailyScan = !!view.querySelector("#enableDailyScan")?.checked;
      cfg.ScanTime = get("#scanTime") || "03:00";
      await Api.updatePluginConfiguration(pluginId, cfg);
      UI && UI.processPluginConfigurationUpdateResult();
      showStatus(view, "Gespeichert.");
    } catch (e) {
      console.error("[CBF] saveConfig", e);
      UI && UI.alert("Speichern fehlgeschlagen.");
    }
  }

  async function scanNow(view) {
    if (!Api) return;
    try {
      const resp = await Api.fetch({ url: Api.getUrl("CollectionsByFolder/ScanNow"), method: "POST" });
      if (!resp.ok) throw new Error("HTTP " + resp.status);
      const json = await resp.json();
      showStatus(view, `Scan gestartet: Kandidaten=${json.candidates}, erstellt=${json.created}, aktualisiert=${json.updated}, übersprungen=${json.skipped}`);
    } catch (e) {
      console.error("[CBF] scanNow", e);
      UI && UI.alert("Scan konnte nicht gestartet werden.");
    }
  }

  function bindOnce(btn, handler) {
    if (btn && !btn._cbfBound) {
      btn.addEventListener("click", (ev) => { ev.preventDefault(); handler(); });
      // Keyboard-Fallback (Enter/Space)
      btn.addEventListener("keydown", (ev) => {
        if (ev.key === "Enter" || ev.key === " ") { ev.preventDefault(); handler(); }
      });
      btn._cbfBound = true;
    }
  }

  return function (view /* HTMLElement */) {
    console.log("[CBF] init", view);

    // Buttons direkt binden
    const saveBtn = view.querySelector("#saveButton");
    const scanBtn = view.querySelector("#scanNowButton");
    console.log("[CBF] buttons", { saveBtn, scanBtn });

    bindOnce(saveBtn, () => saveConfig(view));
    bindOnce(scanBtn, () => scanNow(view));

    // Konfig beim Anzeigen laden
    view.addEventListener("viewshow", function () {
      console.log("[CBF] viewshow -> loadConfig");
      loadConfig(view);
    });

    // Falls bereits sichtbar, direkt laden
    if (view.offsetParent !== null) {
      loadConfig(view);
    }
  };
});
