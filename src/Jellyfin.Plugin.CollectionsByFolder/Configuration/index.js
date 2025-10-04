define(["loading", "emby-button", "emby-input"], function (loading) {
    const pluginId = "f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41"; // deine Plugin-GUID

    /** Lädt aktuelle Plugin-Config vom Server */
    async function loadConfig() {
        const resp = await ApiClient.getPluginConfiguration(pluginId);
        document.querySelector("#cbf-folderPaths").value = resp.FolderPaths?.join(", ") || "";
        document.querySelector("#cbf-prefix").value = resp.Prefix || "";
        document.querySelector("#cbf-suffix").value = resp.Suffix || "";
        document.querySelector("#cbf-blacklist").value = resp.Blacklist?.join(", ") || "";
        document.querySelector("#cbf-minItemCount").value = resp.MinItemCount || 1;
        document.querySelector("#cbf-enableDailyScan").checked = !!resp.EnableDailyScan;
        document.querySelector("#cbf-scanTime").value = resp.ScanTime || "03:00";
    }

    /** Speichert aktuelle Werte in der Plugin-Config */
    async function saveConfig() {
        const cfg = {
            FolderPaths: document.querySelector("#cbf-folderPaths").value.split(",").map(x => x.trim()).filter(Boolean),
            Prefix: document.querySelector("#cbf-prefix").value.trim(),
            Suffix: document.querySelector("#cbf-suffix").value.trim(),
            Blacklist: document.querySelector("#cbf-blacklist").value.split(",").map(x => x.trim()).filter(Boolean),
            MinItemCount: parseInt(document.querySelector("#cbf-minItemCount").value || "1"),
            EnableDailyScan: document.querySelector("#cbf-enableDailyScan").checked,
            ScanTime: document.querySelector("#cbf-scanTime").value
        };

        loading.show();
        await ApiClient.updatePluginConfiguration(pluginId, cfg);
        loading.hide();

        Dashboard.processPluginConfigurationUpdateResult();
        showStatus("Einstellungen gespeichert.");
    }

    /** Startet sofortigen Scan */
    async function scanNow() {
        showStatus("Scan gestartet …");
        try {
            const resp = await ApiClient.fetch({
                url: ApiClient.getUrl("CollectionsByFolder/ScanNow"),
                method: "POST"
            });
            if (resp.ok) {
                showStatus("Scan abgeschlossen.");
            } else {
                showStatus("Fehler beim Scan.");
            }
        } catch (e) {
            showStatus("Fehler: " + e.message);
        }
    }

    /** Zeigt Statusmeldung unten im Formular */
    function showStatus(msg) {
        const el = document.getElementById("cbf-status");
        el.textContent = msg;
        setTimeout(() => (el.textContent = ""), 5000);
    }

    /** Initialisierung beim Anzeigen */
    function initPage() {
        document.querySelector("#cbf-btnSave").addEventListener("click", saveConfig);
        document.querySelector("#cbf-btnScan").addEventListener("click", scanNow);
        loadConfig();
    }

    /** Jellyfin ruft viewshow auf, wenn Seite sichtbar wird */
    window.addEventListener("viewshow", function (e) {
        // kein filter auf view.id, da wir kein data-role="page" haben
        if (e.detail?.view?.querySelector?.("#cbf-folderPaths")) {
            initPage();
        }
    });
});
