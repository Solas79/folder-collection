// Quickfix: blockiert Jellyfins fehlerhaftes scrollTo
window.scrollTo = function (x, y) {
    if (typeof x === "object" && x.behavior === null) {
        x.behavior = "auto";
    }
    Element.prototype.scrollTo.call(window, x, y);
};

define(["loading"], function (loading) {
    "use strict";

    // GUID muss zu deinem Plugin.cs passen:
    const pluginId = "f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41";

    function $(id) { return document.getElementById(id); }

    async function loadConfig() {
        loading.show();
        try {
            const cfg = await ApiClient.getPluginConfiguration(pluginId);
            $("prefix").value = cfg.Prefix || "";
            $("suffix").value = cfg.Suffix || "";
            $("minItems").value = cfg.MinItemCount || 1;
            $("enableDailyScan").checked = !!cfg.EnableDailyScan;
            $("scanTime").value = cfg.ScanTime || "03:00";
            $("blacklist").value = (cfg.Blacklist || []).join(", ");

            // Optional: FolderPaths als verstecktes Feld? (falls du es später anzeigen willst)
            // $("folderPaths").value = (cfg.FolderPaths || []).join(", ");
        } catch (e) {
            console.error("[CollectionsByFolder] loadConfig error:", e);
            Dashboard.alert("Konfiguration konnte nicht geladen werden.");
        } finally {
            loading.hide();
        }
    }

    async function saveConfig() {
        loading.show();
        try {
            const current = await ApiClient.getPluginConfiguration(pluginId);

            const cfg = Object.assign({}, current, {
                Prefix: $("prefix").value.trim(),
                Suffix: $("suffix").value.trim(),
                MinItemCount: parseInt($("minItems").value || "1", 10),
                EnableDailyScan: $("enableDailyScan").checked,
                ScanTime: $("scanTime").value || "03:00",
                Blacklist: $("blacklist").value.split(",").map(s => s.trim()).filter(Boolean),
                // FolderPaths:  (falls du das Feld später im UI hast)
                //   $("folderPaths").value.split(",").map(s => s.trim()).filter(Boolean)
            });

            await ApiClient.updatePluginConfiguration(pluginId, cfg);
            Dashboard.processPluginConfigurationUpdateResult();
            toast("Einstellungen gespeichert.");
        } catch (e) {
            console.error("[CollectionsByFolder] saveConfig error:", e);
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
            toast("Scan gestartet.");
        } catch (e) {
            console.error("[CollectionsByFolder] scanNow error:", e);
            Dashboard.alert("Scan konnte nicht gestartet werden.");
        } finally {
            loading.hide();
        }
    }

    function toast(msg) {
        const el = document.getElementById("cbf-status");
        if (!el) return Dashboard.alert(msg);
        el.textContent = msg;
        clearTimeout(el._t);
        el._t = setTimeout(() => (el.textContent = ""), 4000);
    }

    // Initialisierung: wenn unsere Seite im DOM ist, Buttons binden und laden
    function initWhenReady() {
        const root = document.getElementById("collectionsByFolderPage");
        if (!root) return; // nicht unsere Seite
        const saveBtn = $("saveButton");
        const scanBtn = $("scanNowButton");

        if (saveBtn && !saveBtn._bound) {
            saveBtn._bound = true;
            saveBtn.addEventListener("click", function (e) {
                e.preventDefault();
                saveConfig();
            });
        }
        if (scanBtn && !scanBtn._bound) {
            scanBtn._bound = true;
            scanBtn.addEventListener("click", function (e) {
                e.preventDefault();
                scanNow();
            });
        }
        // Config laden
        loadConfig();
    }

    // Jellyfin feuert bei Seitenwechsel ein viewshow-Event
    window.addEventListener("viewshow", function () {
        // Ohne strikten ID-Filter: init nur, wenn unser Root vorhanden ist
        initWhenReady();
    });

    // Fallback: direkt nach Load einmal versuchen (für iframe-Modus)
    if (document.readyState === "complete" || document.readyState === "interactive") {
        setTimeout(initWhenReady, 0);
    } else {
        document.addEventListener("DOMContentLoaded", initWhenReady);
    }
});
