// Robust für Jellyfin 10.10: Modul gibt eine Funktion zurück, Jellyfin ruft sie mit `view` auf.
define([], function () {
  "use strict";

  const pluginId = "f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41";

  // Hilfen auf Globals (falls vorhanden), ohne sie zu erzwingen:
  const Api = (typeof ApiClient !== "undefined") ? ApiClient : null;
  const UI = (typeof Dashboard !== "undefined") ? Dashboard : null;
  const Loading = (typeof loading !== "undefined") ? loading : null;

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

      const wl = (cfg.Whitelist && cfg.Whitelist.length)
        ? cfg.Whitelist
        : (cfg.FolderPaths || []);
      setLines(view, "whitelist", wl);

      setLines(view, "blacklist", cfg.Blacklist || []);
      (view.querySelector("#prefix") || {}).value = cfg.Prefix || "";
      (view.querySelector("#suffix") || {}).value = cfg.Suffix || "";
      (view.querySelector("#minItems") || {}).value = cfg.MinItemCount || 1;
      const chk = view.querySelector("#enableDailyScan");
      if (chk) chk.checked = !!cfg.EnableDailyScan;
      (view.querySelector("#scanTime") || {}).value = cfg.ScanTime || "03:00";
    } catch (e) {
      console.error("[CBF] loadConfig error", e);
      UI && UI.alert("Konfiguration konnte nicht geladen werden.");
    }
  }

  async function saveConfig(view) {
    if (!Api) return;
    Loading && Loading.show();
    try {
      const cfg = await Api.getPluginConfiguration(pluginId);

      cfg.Whitelist   = linesFrom(view, "whitelist");
      if (cfg.Whitelist?.length) cfg.FolderPaths = []; // Fallback-Feld leeren
      cfg.Blacklist   = linesFrom(view, "blacklist");

      cfg.Prefix = (view.querySelector("#prefix")?.value || "").trim();
      cfg.Suffix = (view.querySelector("#suffix")?.value || "").trim();
      cfg.MinItemCount = parseInt(view.querySelector("#minItems")?.value || "1", 10);
      cfg.EnableDailyScan = !!view.querySelector("#enableDailyScan")?.checked;
      cfg.ScanTime = view.querySelector("#scanTime")?.value || "03:00";

      await Api.updatePluginConfiguration(pluginId, cfg);
      UI && UI.processPluginConfigurationUpdateResult();
      showStatus(view, "Gespeichert.");
    } catch (e) {
      console.error("[CBF] saveConfig error", e);
      UI && UI.alert("Speichern fehlgeschlagen.");
    } finally {
      Loading && Loading.hide();
    }
  }

  async function scanNow(view) {
    if (!Api) return;
    Loading && Loading.show();
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
      console.error("[CBF] scanNow error", e);
      UI && UI.alert("Scan konnte nicht gestartet werden.");
    } finally {
      Loading && Loading.hide();
    }
  }

  function bindButtons(view) {
    // immer über `view` suchen
    const saveBtn = view.querySelector("#saveButton");
    const scanBtn = view.querySelector("#scanNowButton");

    console.log("[CBF] bindButtons", { saveBtn, scanBtn });

    if (saveBtn && !saveBtn._cbfBound) {
      saveBtn.addEventListener("click", (ev) => { ev.preventDefault(); saveConfig(view); });
      saveBtn._cbfBound = true;
    }
    if (scanBtn && !scanBtn._cbfBound) {
      scanBtn.addEventListener("click", (ev) => { ev.preventDefault(); scanNow(view); });
      scanBtn._cbfBound = true;
    }
  }

  // <- WICHTIG: Jellyfin ruft diese Funktion mit der Seiten-Root (`view`) auf
  return function (view /* HTMLElement */, params) {
    console.log("[CBF] page init", view);

    // Beim Öffnen der Seite:
    view.addEventListener("viewshow", function () {
      console.log("[CBF] viewshow");
      bindButtons(view);
      loadConfig(view);
    });

    // Falls die Seite direkt sichtbar ist, sofort binden:
    bindButtons(view);
  };
});
