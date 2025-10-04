// Bulletproof: Event-Delegation am View, keine externen Abhängigkeiten nötig.
define([], function () {
  "use strict";

  const pluginId = "f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41";

  // weiche Globals (falls vorhanden)
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

      // Whitelist bevorzugt, FolderPaths als Fallback
      const wl = (cfg.Whitelist && cfg.Whitelist.length)
        ? cfg.Whitelist
        : (cfg.FolderPaths || []);
      setLines(view, "whitelist", wl);

      setLines(view, "blacklist", cfg.Blacklist || []);
      (view.querySelector("#prefix")    || {}).value = cfg.Prefix || "";
      (view.querySelector("#suffix")    || {}).value = cfg.Suffix || "";
      (view.querySelector("#minItems")  || {}).value = cfg.MinItemCount || 1;
      const chk = view.querySelector("#enableDailyScan");
      if (chk) chk.checked = !!cfg.EnableDailyScan;
      (view.querySelector("#scanTime")  || {}).value = cfg.ScanTime || "03:00";
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

      cfg.Prefix = (view.querySelector("#prefix")?.value || "").trim();
      cfg.Suffix = (view.querySelector("#suffix")?.value || "").trim();
      cfg.MinItemCount = parseInt(view.querySelector("#minItems")?.value || "1", 10);
      cfg.EnableDailyScan = !!view.querySelector("#enableDailyScan")?.checked;
      cfg.ScanTime = view.querySelector("#scanTime")?.value || "03:00";

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
      const resp = await Api.fetch({
        url: Api.getUrl("CollectionsByFolder/ScanNow"),
        method: "POST"
      });
      if (!resp.ok) throw new Error("HTTP " + resp.status);
      const json = await resp.json();
      showStatus(
        view,
        `Scan gestartet: Kandidaten=${json.candidates}, erstellt=${json.created}, aktualisiert=${json.updated}, übersprungen=${json.skipped}`
      );
    } catch (e) {
      console.error("[CBF] scanNow", e);
      UI && UI.alert("Scan konnte nicht gestartet werden.");
    }
  }

  // WICHTIG: Jellyfin ruft diese Funktion mit dem Seiten-Root `view` auf
  return function (view /* HTMLElement */, params) {
    console.log("[CBF] init", view);

    // 1) Event-Delegation: klicks auf der Seite abfangen
    view.addEventListener("click", function (ev) {
      const saveBtn = ev.target.closest && ev.target.closest("#saveButton");
      if (saveBtn) {
        ev.preventDefault();
        saveConfig(view);
        return;
      }
      const scanBtn = ev.target.closest && ev.target.closest("#scanNowButton");
      if (scanBtn) {
        ev.preventDefault();
        scanNow(view);
        return;
      }
    });

    // 2) Beim Anzeigen der Seite Konfig laden
    view.addEventListener("viewshow", function () {
      console.log("[CBF] viewshow");
      loadConfig(view);
    });

    // Falls bereits sichtbar: sofort laden
    if (view.offsetParent !== null) {
      loadConfig(view);
    }
  };
});
