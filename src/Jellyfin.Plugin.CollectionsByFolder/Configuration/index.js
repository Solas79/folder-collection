define(["loading"], function (loading) {
  "use strict";

  const pluginId = "f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41";

  // Jellyfin lädt das AMD-Modul und ruft diese Funktion mit dem Seiten-root `view` auf
  return function (view /* HTMLElement */) {
    const $ = (sel) => view.querySelector(sel);

    function showStatus(msg) {
      const el = $("#cbf-status");
      if (!el) return;
      el.textContent = msg;
      clearTimeout(el._t);
      el._t = setTimeout(() => (el.textContent = ""), 4000);
    }

    const linesFrom = (id) => ($("#" + id)?.value || "")
      .split("\n").map(s=>s.trim()).filter(Boolean);

    const setLines = (id, arr) => {
      const el = $("#"+id);
      if (el) el.value = (arr || []).join("\n");
    };

    async function loadConfig() {
      try {
        const cfg = await ApiClient.getPluginConfiguration(pluginId);

        const wl = (cfg.Whitelist?.length ? cfg.Whitelist : (cfg.FolderPaths || []));
        setLines("whitelist", wl);
        setLines("blacklist", cfg.Blacklist || []);

        $("#prefix").value    = cfg.Prefix || "";
        $("#suffix").value    = cfg.Suffix || "";
        $("#minItems").value  = cfg.MinItemCount || 1;
        $("#enableDailyScan").checked = !!cfg.EnableDailyScan;
        $("#scanTime").value  = cfg.ScanTime || "03:00";
      } catch (e) {
        console.error("[CBF] loadConfig", e);
        Dashboard.alert("Konfiguration konnte nicht geladen werden.");
      }
    }

    async function saveConfig() {
      loading.show();
      try {
        const cfg = await ApiClient.getPluginConfiguration(pluginId);

        cfg.Whitelist = linesFrom("whitelist");
        if (cfg.Whitelist?.length) cfg.FolderPaths = []; // alten Key leeren, wenn Whitelist genutzt
        cfg.Blacklist = linesFrom("blacklist");

        cfg.Prefix = $("#prefix").value.trim();
        cfg.Suffix = $("#suffix").value.trim();
        cfg.MinItemCount = parseInt($("#minItems").value || "1", 10);
        cfg.EnableDailyScan = $("#enableDailyScan").checked;
        cfg.ScanTime = $("#scanTime").value || "03:00";

        await ApiClient.updatePluginConfiguration(pluginId, cfg);
        Dashboard.processPluginConfigurationUpdateResult();
        showStatus("Gespeichert.");
      } catch(e) {
        console.error("[CBF] saveConfig", e);
        Dashboard.alert("Speichern fehlgeschlagen.");
      } finally {
        loading.hide();
      }
    }

    async function scanNow() {
      loading.show();
      try {
        const resp = await ApiClient.fetch({
          url: ApiClient.getUrl("CollectionsByFolder/ScanNow"),
          method: "POST"
        });
        if (!resp.ok) throw new Error("HTTP " + resp.status);
        const json = await resp.json();
        showStatus(`Scan gestartet: Kandidaten=${json.candidates}, erstellt=${json.created}, aktualisiert=${json.updated}, übersprungen=${json.skipped}`);
      } catch (e) {
        console.error("[CBF] scanNow", e);
        Dashboard.alert("Scan konnte nicht gestartet werden.");
      } finally {
        loading.hide();
      }
    }

    function bind() {
      const saveBtn = $("#saveButton");
      const scanBtn = $("#scanNowButton");
      if (saveBtn && !saveBtn._cbfBound) { saveBtn.addEventListener("click", saveConfig); saveBtn._cbfBound = true; }
      if (scanBtn && !scanBtn._cbfBound) { scanBtn.addEventListener("click", scanNow);  scanBtn._cbfBound = true; }
    }

    // Beim Anzeigen der Seite initialisieren
    view.addEventListener("viewshow", function () {
      bind();
      loadConfig();
    });

    // Falls bereits sichtbar (direkt aufgerufen), sofort binden/laden
    bind();
    if (view.offsetParent !== null) loadConfig();
  };
});
