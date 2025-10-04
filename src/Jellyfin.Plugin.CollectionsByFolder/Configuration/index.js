define([], function () {
  "use strict";
  const pluginId = "f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41";

  return function (view) {
    console.log("[CBF] init", view);

    const $ = (sel) => view.querySelector(sel);
    const status = $("#cbf-status");

    function show(msg){ if (status) { status.textContent = msg; setTimeout(()=>status.textContent="",4000); } }

    async function save() {
      const cfg = await ApiClient.getPluginConfiguration(pluginId);
      cfg.Whitelist = ($("#whitelist").value || "").split("\n").map(s=>s.trim()).filter(Boolean);
      cfg.Blacklist = ($("#blacklist").value || "").split("\n").map(s=>s.trim()).filter(Boolean);
      cfg.Prefix = ($("#prefix").value || "").trim();
      cfg.Suffix = ($("#suffix").value || "").trim();
      await ApiClient.updatePluginConfiguration(pluginId, cfg);
      Dashboard.processPluginConfigurationUpdateResult();
      show("Gespeichert.");
    }

    async function scan() {
      const resp = await ApiClient.fetch({ url: ApiClient.getUrl("CollectionsByFolder/ScanNow"), method:"POST" });
      if (!resp.ok) throw new Error("HTTP "+resp.status);
      const json = await resp.json();
      show(`Scan gestartet: Kandidaten=${json.candidates}, erstellt=${json.created}, aktualisiert=${json.updated}, übersprungen=${json.skipped}`);
    }

    // Buttons binden
    const saveBtn = $("#saveButton");
    const scanBtn = $("#scanNowButton");
    if (saveBtn && !saveBtn._cbf) { saveBtn.addEventListener("click", save); saveBtn._cbf = 1; }
    if (scanBtn && !scanBtn._cbf) { scanBtn.addEventListener("click", scan); scanBtn._cbf = 1; }

    // beim Anzeigen laden
    view.addEventListener("viewshow", async () => {
      try {
        const cfg = await ApiClient.getPluginConfiguration(pluginId);
        $("#whitelist").value = (cfg.Whitelist?.join("\n")) || (cfg.FolderPaths?.join("\n")) || "";
        $("#blacklist").value = (cfg.Blacklist?.join("\n")) || "";
        $("#prefix").value    = cfg.Prefix || "";
        $("#suffix").value    = cfg.Suffix || "";
      } catch(e) { console.error("[CBF] load error", e); }
    });
  };
});
