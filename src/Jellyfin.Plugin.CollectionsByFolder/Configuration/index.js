// src/Jellyfin.Plugin.CollectionsByFolder/Configuration/index.js
define(["loading"], function (loading) {
  "use strict";

  const pluginId = "f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41";

  // Jellyfin lädt dieses Modul und ruft die zurückgegebene Funktion
  // mit dem Seiten-Root-Element `view` auf.
  return function (view /* HTMLElement */, params) {
    console.log("[CBF] page init", view);

    const $ = (id) => view.querySelector("#" + id);

    function status(msg) {
      const el = $("#cbf-status");
      if (!el) return;
      el.textContent = msg;
      clearTimeout(el._t);
      el._t = setTimeout(() => (el.textContent = ""), 5000);
    }

    function linesFromTextarea(id) {
      const el = $("#" + id) || $(id); // robust
      return (el?.value || "")
        .split("\n")
        .map((s) => s.trim())
        .filter(Boolean);
    }

    function setTextareaLines(id, arr) {
      const el = $("#" + id) || $(id);
      if (el) el.value = (arr || []).join("\n");
    }

    async function loadConfig() {
      try {
        const cfg = await ApiClient.getPluginConfiguration(pluginId);

        // Whitelist bevorzugt; FolderPaths als Fallback anzeigen
        const wl = (cfg.Whitelist && cfg.Whitelist.length)
          ? cfg.Whitelist
          : (cfg.FolderPaths || []);
        setTextareaLines("whitelist", wl);

        setTextareaLines("blacklist", cfg.Blacklist || []);
        $("#prefix").value = cfg.Prefix || "";
        $("#suffix").value = cfg.Suffix || "";
        $("#minItems").value = cfg.MinItemCount || 1;
        $("#enableDailyScan").checked = !!cfg.EnableDailyScan;
        $("#scanTime").value = cfg.ScanTime || "03:00";
      } catch (e) {
        console.error("[CBF] loadConfig error", e);
        Dashboard.alert("Konfiguration konnte nicht geladen werden.");
      }
    }

    async function saveConfig() {
      loading.show();
      try {
        const cfg = await ApiClient.getPluginConfiguration(pluginId);

        cfg.Whitelist   = linesFromTextarea("whitelist");
        // optional: FolderPaths leeren, wenn Whitelist genutzt wird
        if (cfg.Whitelist?.length) cfg.FolderPaths = [];

        cfg.Blacklist   = linesFromTextarea("blacklist");
        cfg.Prefix = $("#prefix").value.trim();
        cfg.Suffix = $("#suffix").value.trim();
        cfg.MinItemCount = parseInt($("#minItems").value || "1", 10);
        cfg.EnableDailyScan = $("#enableDailyScan").checked;
        cfg.ScanTime = $("#scanTime").value || "03:00";

        await ApiClient.updatePluginConfiguration(pluginId, cfg);
        Dashboard.processPluginConfigurationUpdateResult();
        status("Gespeichert.");
      } catch (e) {
        console.error("[CBF] saveConfig error", e);
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
          method: "POST",
        });
        if (!resp.ok) throw new Error("HTTP " + resp.status);
        const json = await resp.json();
        status(
          `Scan gestartet: Kandidaten=${json.candidates}, erstellt=${json.created}, aktualisiert=${json.updated}, übersprungen=${json.skipped}`
        );
      } catch (e) {
        console.error("[CBF] scanNow error", e);
        Dashboard.alert("Scan konnte nicht gestartet werden.");
      } finally {
        loading.hide();
      }
    }

    // Buttons **auf dieser Seite** binden (immer über `view`)
    function bindButtons() {
      const saveBtn = $("#saveButton");
      const scanBtn = $("#scanNowButton");
      if (saveBtn && !saveBtn._cbfBound) {
        saveBtn.addEventListener("click", (ev) => { ev.preventDefault(); saveConfig(); });
        saveBtn._cbfBound = true;
      }
      if (scanBtn && !scanBtn._cbfBound) {
        scanBtn.addEventListener("click", (ev) => { ev.preventDefault(); scanNow(); });
        scanBtn._cbfBound = true;
      }
    }

    // Wird aufgerufen, wenn die Seite sichtbar wird
    view.addEventListener("viewshow", function () {
      console.log("[CBF] viewshow");
      bindButtons();
      loadConfig();
    });

    // Falls Jellyfin die Seite initial „sichtbar“ einbettet
    bindButtons();
    // Konfig lädt erst bei viewshow — wenn du sofort laden willst, hier zusätzlich:
    // loadConfig();
  };
});
