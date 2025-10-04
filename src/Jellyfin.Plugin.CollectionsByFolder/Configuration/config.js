define(["loading", "emby-button", "emby-input"], function (loading) {
  "use strict";

  const pluginId = "f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41";

  function $id(view, id) {
    return view.querySelector("#" + id);
  }

  function loadConfig(view) {
    loading.show();
    return ApiClient.getPluginConfiguration(pluginId)
      .then(cfg => {
        $id(view, "cbf-folderPaths").value   = (cfg.FolderPaths || []).join(", ");
        $id(view, "cbf-prefix").value        = cfg.Prefix || "";
        $id(view, "cbf-suffix").value        = cfg.Suffix || "";
        $id(view, "cbf-blacklist").value     = (cfg.Blacklist || []).join(", ");
        $id(view, "cbf-minItemCount").value  = cfg.MinItemCount || 1;
        $id(view, "cbf-enableDailyScan").checked = !!cfg.EnableDailyScan;
        $id(view, "cbf-scanTime").value      = cfg.ScanTime || "00:00";
      })
      .catch(err => {
        console.error("[CollectionsByFolder] loadConfig:", err);
        Dashboard.alert("Konfiguration konnte nicht geladen werden.");
      })
      .finally(() => loading.hide());
  }

  function saveConfig(view) {
    loading.show();
    return ApiClient.getPluginConfiguration(pluginId)
      .then(cfg => {
        cfg.FolderPaths   = $id(view, "cbf-folderPaths").value.split(",").map(s => s.trim()).filter(Boolean);
        cfg.Prefix        = ($id(view, "cbf-prefix").value || "").trim();
        cfg.Suffix        = ($id(view, "cbf-suffix").value || "").trim();
        cfg.Blacklist     = $id(view, "cbf-blacklist").value.split(",").map(s => s.trim()).filter(Boolean);
        cfg.MinItemCount  = parseInt($id(view, "cbf-minItemCount").value || "1", 10);
        cfg.EnableDailyScan = $id(view, "cbf-enableDailyScan").checked;
        cfg.ScanTime      = $id(view, "cbf-scanTime").value || "00:00";

        return ApiClient.updatePluginConfiguration(pluginId, cfg);
      })
      .then(() => {
        Dashboard.processPluginConfigurationUpdateResult();
        const s = $id(view, "cbf-status");
        if (s) s.innerHTML = "<div class='alert alert-success'>Gespeichert!</div>";
      })
      .catch(err => {
        console.error("[CollectionsByFolder] saveConfig:", err);
        Dashboard.alert("Speichern fehlgeschlagen.");
      })
      .finally(() => loading.hide());
  }

  function scanNow(view) {
    loading.show();
    return ApiClient.ajax({
      type: "POST",
      url: ApiClient.getUrl("CollectionsByFolder/ScanNow")
    })
      .then(() => {
        const s = $id(view, "cbf-status");
        if (s) s.innerHTML = "<div class='alert alert-info'>Scan gestartet!</div>";
      })
      .catch(err => {
        console.error("[CollectionsByFolder] scanNow:", err);
        Dashboard.alert("Scan konnte nicht gestartet werden.");
      })
      .finally(() => loading.hide());
  }

  // Jellyfin initialisiert AMD-Module mit (view, params) auf viewshow
  return function (view) {
    view.addEventListener("viewshow", function () {
      // Nur unsere Seite initialisieren
      if (view.id !== "cbf-Page") return;

      loadConfig(view);

      const btnSave = $id(view, "cbf-btnSave");
      const btnScan = $id(view, "cbf-btnScan");

      if (btnSave && !btnSave._bound) {
        btnSave._bound = true;
        btnSave.addEventListener("click", () => saveConfig(view));
      }
      if (btnScan && !btnScan._bound) {
        btnScan._bound = true;
        btnScan.addEventListener("click", () => scanNow(view));
      }
    });
  };
});
