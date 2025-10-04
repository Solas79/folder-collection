console.log("[CBF] index.js LOADED");

define(["loading"], function (loading) {
  "use strict";
  const pluginId = "f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41";
  const $ = (id) => document.getElementById(id);

  function status(msg) {
    const el = $("cbf-status");
    if (!el) return;
    el.textContent = msg;
    clearTimeout(el._t);
    el._t = setTimeout(() => (el.textContent = ""), 4000);
  }

  async function loadConfig() {
    try {
      const cfg = await ApiClient.getPluginConfiguration(pluginId);
      $("prefix").value = cfg.Prefix || "";
      $("suffix").value = cfg.Suffix || "";
      $("minItems").value = cfg.MinItemCount || 1;
      $("enableDailyScan").checked = !!cfg.EnableDailyScan;
      $("scanTime").value = cfg.ScanTime || "03:00";
      $("blacklist").value = (cfg.Blacklist || []).join(", ");
      $("folderPaths").value = (cfg.FolderPaths || []).join("\n");
    } catch (e) {
      console.error("[CBF] loadConfig", e);
      Dashboard.alert("Konfiguration konnte nicht geladen werden.");
    }
  }

  async function saveConfig() {
    loading.show();
    try {
      const cfg = await ApiClient.getPluginConfiguration(pluginId);
      cfg.Prefix = $("prefix").value.trim();
      cfg.Suffix = $("suffix").value.trim();
      cfg.MinItemCount = parseInt($("minItems").value || "1", 10);
      cfg.EnableDailyScan = $("enableDailyScan").checked;
      cfg.ScanTime = $("scanTime").value || "03:00";
      cfg.Blacklist = $("blacklist").value.split(",").map(s=>s.trim()).filter(Boolean);
      cfg.FolderPaths = $("folderPaths").value.split("\n").map(s=>s.trim()).filter(Boolean);

      await ApiClient.updatePluginConfiguration(pluginId, cfg);
      Dashboard.processPluginConfigurationUpdateResult();
      status("Gespeichert.");
    } catch (e) {
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
      status(`Scan gestartet: Kandidaten=${json.candidates}, erstellt=${json.created}, aktualisiert=${json.updated}, übersprungen=${json.skipped}`);
    } catch (e) {
      console.error("[CBF] scanNow", e);
      Dashboard.alert("Scan konnte nicht gestartet werden.");
    } finally {
      loading.hide();
    }
  }

  function bind() {
    const root = document.getElementById("collectionsByFolderPage");
    if (!root) return;
    $("saveButton").addEventListener("click", e => { e.preventDefault(); saveConfig(); });
    $("scanNowButton").addEventListener("click", e => { e.preventDefault(); scanNow(); });
    loadConfig();
  }

  window.addEventListener("viewshow", bind);
  if (document.readyState !== "loading") bind();
  else document.addEventListener("DOMContentLoaded", bind);
});
