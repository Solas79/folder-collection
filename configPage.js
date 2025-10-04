define([], function () {
  "use strict";

  const pluginId = "f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41";

  return function (view) {
    const $ = (sel) => view.querySelector(sel);
    const status = $("#cbf-status");

    function show(msg) { if (status) { status.textContent = msg; setTimeout(() => status.textContent = "", 3000); } }

    async function load() {
      try {
        const cfg = await ApiClient.getPluginConfiguration(pluginId);
        $("#whitelist").value = (cfg.Whitelist || cfg.FolderPaths || []).join("\n");
        $("#blacklist").value = (cfg.Blacklist || []).join("\n");
        $("#prefix").value = cfg.Prefix || "";
        $("#suffix").value = cfg.Suffix || "";
        $("#minfiles").value = cfg.MinFiles || 2;
      } catch (e) {
        console.error("[CBF] load error", e);
      }
    }

    async function save() {
      const cfg = await ApiClient.getPluginConfiguration(pluginId);
      cfg.Whitelist = ($("#whitelist").value || "").split("\n").map(s => s.trim()).filter(Boolean);
      cfg.Blacklist = ($("#blacklist").value || "").split("\n").map(s => s.trim()).filter(Boolean);
      cfg.Prefix = ($("#prefix").value || "").trim();
      cfg.Suffix = ($("#suffix").value || "").trim();
      cfg.MinFiles = parseInt($("#minfiles").value || "2", 10);
      await ApiClient.updatePluginConfiguration(pluginId, cfg);
      Dashboard.processPluginConfigurationUpdateResult();
      show("Gespeichert.");
    }

    async function scan() {
      try {
        const resp = await ApiClient.fetch({ url: ApiClient.getUrl("CollectionsByFolder/ScanNow"), method: "POST" });
        const info = await resp.json().catch(() => ({}));
        show(`Scan gestartet${info?.candidates != null ? " (Kandidaten: "+info.candidates+")" : ""}.`);
      } catch (e) {
        console.error("[CBF] scan error", e);
        show("Scan-Start fehlgeschlagen.");
      }
    }

    const saveBtn = $("#saveButton");
    const scanBtn = $("#scanNowButton");
    if (saveBtn && !saveBtn._cbf) { saveBtn.addEventListener("click", save); saveBtn._cbf = 1; }
    if (scanBtn && !scanBtn._cbf) { scanBtn.addEventListener("click", scan); scanBtn._cbf = 1; }

    view.addEventListener("viewshow", load);
  };
});
