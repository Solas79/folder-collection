define(["loading", "emby-button", "emby-input"], function (loading) {
    "use strict";

    const pluginId = "f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41";

    function loadConfig(view) {
        loading.show();
        return ApiClient.getPluginConfiguration(pluginId)
            .then(cfg => {
                view.querySelector("#folderPaths").value = (cfg.FolderPaths || []).join(", ");
                view.querySelector("#prefix").value      = cfg.Prefix || "";
                view.querySelector("#suffix").value      = cfg.Suffix || "";
                view.querySelector("#blacklist").value   = (cfg.Blacklist || []).join(", ");
                view.querySelector("#minItemCount").value = cfg.MinItemCount || 1;
                view.querySelector("#enableDailyScan").checked = !!cfg.EnableDailyScan;
                view.querySelector("#scanTime").value    = cfg.ScanTime || "00:00";
            })
            .catch(err => {
                console.error("[CollectionsByFolder] loadConfig error:", err);
                Dashboard.alert("Konfiguration konnte nicht geladen werden.");
            })
            .finally(() => loading.hide());
    }

    function saveConfig(view) {
        loading.show();
        return ApiClient.getPluginConfiguration(pluginId)
            .then(cfg => {
                cfg.FolderPaths    = view.querySelector("#folderPaths").value.split(",").map(s => s.trim()).filter(Boolean);
                cfg.Prefix         = (view.querySelector("#prefix").value || "").trim();
                cfg.Suffix         = (view.querySelector("#suffix").value || "").trim();
                cfg.Blacklist      = view.querySelector("#blacklist").value.split(",").map(s => s.trim()).filter(Boolean);
                cfg.MinItemCount   = parseInt(view.querySelector("#minItemCount").value || "1", 10);
                cfg.EnableDailyScan = view.querySelector("#enableDailyScan").checked;
                cfg.ScanTime       = view.querySelector("#scanTime").value || "00:00";

                return ApiClient.updatePluginConfiguration(pluginId, cfg);
            })
            .then(() => {
                Dashboard.processPluginConfigurationUpdateResult();
                const s = view.querySelector("#saveStatus");
                if (s) s.innerHTML = "<div class='alert alert-success'>Gespeichert!</div>";
            })
            .catch(err => {
                console.error("[CollectionsByFolder] saveConfig error:", err);
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
            const s = view.querySelector("#saveStatus");
            if (s) s.innerHTML = "<div class='alert alert-info'>Scan gestartet!</div>";
        })
        .catch(err => {
            console.error("[CollectionsByFolder] scanNow error:", err);
            Dashboard.alert("Scan konnte nicht gestartet werden.");
        })
        .finally(() => loading.hide());
    }

    // Wichtig: AMD-Module geben eine Initializer-Funktion zurück,
    // Jellyfin ruft diese mit (view, params) auf, wenn die Page angezeigt wird.
    return function (view /*, params */) {
        view.addEventListener("viewshow", function () {
            loadConfig(view);

            const btnSave = view.querySelector("#btnSaveConfig");
            const btnScan = view.querySelector("#btnScanNow");

            if (btnSave && !btnSave._bound) {
                btnSave._bound = true;
                btnSave.addEventListener("click", function () {
                    saveConfig(view);
                });
            }
            if (btnScan && !btnScan._bound) {
                btnScan._bound = true;
                btnScan.addEventListener("click", function () {
                    scanNow(view);
                });
            }
        });
    };
});
