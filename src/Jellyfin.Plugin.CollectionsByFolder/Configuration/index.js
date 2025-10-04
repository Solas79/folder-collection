console.log("[CBF] index.js LOADED");

define(["loading"], function (loading) {
  "use strict";
  const pluginId = "f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41";

  function $(id){ return document.getElementById(id); }

  async function loadConfig(){
    const cfg = await ApiClient.getPluginConfiguration(pluginId);
    $("prefix").value = cfg.Prefix || "";
    $("suffix").value = cfg.Suffix || "";
    $("minItems").value = cfg.MinItemCount || 1;
    $("enableDailyScan").checked = !!cfg.EnableDailyScan;
    $("scanTime").value = cfg.ScanTime || "03:00";
    $("blacklist").value = (cfg.Blacklist||[]).join(", ");
  }
  async function saveConfig(){
    const cfg = await ApiClient.getPluginConfiguration(pluginId);
    cfg.Prefix = $("prefix").value.trim();
    cfg.Suffix = $("suffix").value.trim();
    cfg.MinItemCount = parseInt($("minItems").value||"1",10);
    cfg.EnableDailyScan = $("enableDailyScan").checked;
    cfg.ScanTime = $("scanTime").value||"03:00";
    cfg.Blacklist = $("blacklist").value.split(",").map(s=>s.trim()).filter(Boolean);
    await ApiClient.updatePluginConfiguration(pluginId, cfg);
    Dashboard.processPluginConfigurationUpdateResult();
    $("cbf-status").textContent = "Gespeichert";
  }
  async function scanNow(){
    await ApiClient.fetch({ url: ApiClient.getUrl("CollectionsByFolder/ScanNow"), method:"POST" });
    $("cbf-status").textContent = "Scan gestartet";
  }

  function bind(){
    const root = document.getElementById("collectionsByFolderPage");
    if (!root) return;
    document.getElementById("saveButton").addEventListener("click", e => { e.preventDefault(); saveConfig(); });
    document.getElementById("scanNowButton").addEventListener("click", e => { e.preventDefault(); scanNow(); });
    loadConfig();
  }

  window.addEventListener("viewshow", bind);
  if (document.readyState !== "loading") bind();
  else document.addEventListener("DOMContentLoaded", bind);
});
