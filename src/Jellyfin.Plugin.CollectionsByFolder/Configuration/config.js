(() => {
  const pluginId = "f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41";

  function loadConfig(page) {
    Dashboard.showLoadingMsg();
    return ApiClient.getPluginConfiguration(pluginId)
      .then(cfg => {
        page.querySelector("#folderPaths").value = (cfg.FolderPaths || []).join(", ");
        page.querySelector("#prefix").value      = cfg.Prefix || "";
        page.querySelector("#suffix").value      = cfg.Suffix || "";
        page.querySelector("#blacklist").value   = (cfg.Blacklist || []).join(", ");
        page.querySelector("#minItemCount").value = cfg.MinItemCount || 1;
        page.querySelector("#enableDailyScan").checked = !!cfg.EnableDailyScan;
        page.querySelector("#scanTime").value    = cfg.ScanTime || "00:00";
      })
      .catch(err => {
        console.error("[CollectionsByFolder] loadConfig error:", err);
        Dashboard.alert("Konfiguration konnte nicht geladen werden.");
      })
      .finally(() => Dashboard.hideLoadingMsg());
  }

  function saveConfig(page) {
    Dashboard.showLoadingMsg();
    return ApiClient.getPluginConfiguration(pluginId)
      .then(cfg => {
        cfg.FolderPaths   = page.querySelector("#folderPaths").value.split(",").map(s => s.trim()).filter(Boolean);
        cfg.Prefix        = (page.querySelector("#prefix").value || "").trim();
        cfg.Suffix        = (page.querySelector("#suffix").value || "").trim();
        cfg.Blacklist     = page.querySelector("#blacklist").value.split(",").map(s => s.trim()).filter(Boolean);
        cfg.MinItemCount  = parseInt(page.querySelector("#minItemCount").value || "1", 10);
        cfg.EnableDailyScan = page.querySelector("#enableDailyScan").checked;
        cfg.ScanTime      = page.querySelector("#scanTime").value || "00:00";

        return ApiClient.updatePluginConfiguration(pluginId, cfg);
      })
      .then(() => {
        Dashboard.processPluginConfigurationUpdateResult();
        const s = page.querySelector("#saveStatus");
        if (s) s.innerHTML = "<div class='alert alert-success'>Gespeichert!</div>";
      })
      .catch(err => {
        console.error("[CollectionsByFolder] saveConfig error:", err);
        Dashboard.alert("Speichern fehlgeschlagen.");
      })
      .finally(() => Dashboard.hideLoadingMsg());
  }

  function scanNow(page) {
    Dashboard.showLoadingMsg();
    return ApiClient.ajax({
      type: "POST",
      url: ApiClient.getUrl("CollectionsByFolder/ScanNow")
    })
      .then(() => {
        const s = page.querySelector("#saveStatus");
        if (s) s.innerHTML = "<div class='alert alert-info'>Scan gestartet!</div>";
      })
      .catch(err => {
        console.error("[CollectionsByFolder] scanNow error:", err);
        Dashboard.alert("Scan konnte nicht gestartet werden.");
      })
      .finally(() => Dashboard.hideLoadingMsg());
  }

  // Jellyfin ruft die Seite via jQuery Mobile auf → auf pageshow warten:
  document.addEventListener("pageshow", ev => {
    const page = ev.target;
    if (!page || page.id !== "collectionsbyfolderPage") return;

    // initiales Laden
    loadConfig(page);

    // Buttons binden
    const btnSave = page.querySelector("#btnSaveConfig");
    const btnScan = page.querySelector("#btnScanNow");

    if (btnSave && !btnSave._bound) {
      btnSave._bound = true;
      btnSave.addEventListener("click", () => saveConfig(page));
    }
    if (btnScan && !btnScan._bound) {
      btnScan._bound = true;
      btnScan.addEventListener("click", () => scanNow(page));
    }
  });
})();
