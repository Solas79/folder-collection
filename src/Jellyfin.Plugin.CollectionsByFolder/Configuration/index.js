// src/Jellyfin.Plugin.CollectionsByFolder/Configuration/index.js
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
    el._t = setTimeout(() => (el.textContent = ""), 5000);
  }

  function linesFromTextarea(id) {
    return ($(id).value || "")
      .split("\n")
      .map(s => s.trim())
      .filter(Boolean);
  }

  function setTextareaLines(id, arr) {
    $(id).value = (arr || []).join("\n");
  }

  async function loadConfig() {
    try {
      const cfg = await ApiClient.getPluginConfiguration(pluginId);

      // Whitelist bevorzugt; falls leer, alten Key FolderPaths anzeigen
      const wl = (cfg.Whitelist && cfg.Whitelist.length) ? cfg.Whitelist : (cfg.FolderPaths || []);
      setTextareaLines("whitelist", wl);

      setTextareaLines("blacklist", cfg.Blacklist || []);
      $("prefix").value = cfg.Prefix || "";
      $("suffix").value = cfg.Suffix || "";
      $("minItems").value = cfg.MinItemCount || 1;
      $("enableDailyScan").checked = !!cfg.EnableDailyScan;
      $("scanTime").value = cfg.ScanTime || "03:00";
    } catch (e) {
      console.error("[CBF] loadConfig", e);
      Dashboard.alert("Konfiguration konnte nicht geladen werden.");
    }
  }

  async function saveConfig() {
    loading.show();
    try {
      const cfg = await ApiClient.getPluginConfiguration(pluginId);

      cfg.Whitelist   = linesFromTextarea("whitelist");
      cfg.FolderPaths = cfg.Whitelist && cfg.Whitelist.length ? [] : (cfg.FolderPaths || []); // optional: leeren, wenn Whitelist genutzt wird
      cfg.Blacklist   = linesFromTextarea("blacklist");

      cfg.Prefix = $("prefix").value.trim();
      cfg.Suffix = $("suffix").value.trim();
      cfg.MinItemCount = parseInt($("minItems").value || "1", 10);
      cfg.EnableDailyScan = $("enableDailyScan").checked;
      cfg.ScanTime = $("scanTime").value || "03:00";

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
