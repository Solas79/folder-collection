define(["globalize", "loading", "emby-button", "emby-input"], function (globalize, loading) {

    const pluginId = "f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41";

    function loadConfiguration(page) {
        loading.show();
        ApiClient.getPluginConfiguration(pluginId).then(config => {
            page.querySelector("#folderPaths").value = (config.FolderPaths || []).join(", ");
            page.querySelector("#prefix").value = config.Prefix || "";
            page.querySelector("#suffix").value = config.Suffix || "";
            page.querySelector("#blacklist").value = (config.Blacklist || []).join(", ");
            page.querySelector("#minItemCount").value = config.MinItemCount || 1;
            page.querySelector("#enableDailyScan").checked = !!config.EnableDailyScan;
            page.querySelector("#scanTime").value = config.ScanTime || "00:00";
            loading.hide();
        });
    }

    function saveConfiguration(page) {
        loading.show();
        ApiClient.getPluginConfiguration(pluginId).then(config => {
            config.FolderPaths = page.querySelector("#folderPaths").value.split(",").map(s => s.trim()).filter(Boolean);
            config.Prefix = (page.querySelector("#prefix").value || "").trim();
            config.Suffix = (page.querySelector("#suffix").value || "").trim();
            config.Blacklist = page.querySelector("#blacklist").value.split(",").map(s => s.trim()).filter(Boolean);
            config.MinItemCount = parseInt(page.querySelector("#minItemCount").value || "1", 10);
            config.EnableDailyScan = page.querySelector("#enableDailyScan").checked;
            config.ScanTime = page.querySelector("#scanTime").value || "00:00";

            return ApiClient.updatePluginConfiguration(pluginId, config).then(() => {
                Dashboard.processPluginConfigurationUpdateResult();
                page.querySelector("#saveStatus").innerHTML = "<div class='alert alert-success'>Gespeichert!</div>";
                loading.hide();
            });
        });
    }

    function scanNow(page) {
        loading.show();
        fetch(ApiClient.getUrl("CollectionsByFolder/ScanNow"), { method: "POST" })
            .then(() => {
                page.querySelector("#saveStatus").innerHTML = "<div class='alert alert-info'>Scan gestartet!</div>";
                loading.hide();
            })
            .catch(() => {
                page.querySelector("#saveStatus").innerHTML = "<div class='alert alert-error'>Fehler beim Starten!</div>";
                loading.hide();
            });
    }

    return function (view) {
        view.addEventListener("viewshow", function () {
            loadConfiguration(view);
            view.querySelector("#btnSaveConfig").addEventListener("click", function () {
                saveConfiguration(view);
            });
            view.querySelector("#btnScanNow").addEventListener("click", function () {
                scanNow(view);
            });
        });
    };
});
