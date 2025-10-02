(() => {
  // GUID exakt wie in Plugin.cs & manifest.json
  const pluginId = "9f4f2c47-b3c5-4b13-9b1f-1c9a5c3b8d6a";

  function toLines(arr) { return Array.isArray(arr) ? arr.join("\n") : (arr || ""); }
  function fromLines(text) { return (text || "").split("\n").map(s => s.trim()).filter(Boolean); }

  async function load() {
    const cfg = await ApiClient.getPluginConfiguration(pluginId);

    document.getElementById("includeMovies").checked = !!cfg.IncludeMovies;
    document.getElementById("includeSeries").checked = !!cfg.IncludeSeries;
    document.getElementById("minItems").value = cfg.MinItems ?? 2;
    document.getElementById("prefix").value = cfg.Prefix ?? "";
    document.getElementById("suffix").value = cfg.Suffix ?? "";
    document.getElementById("scanHour").value = cfg.ScanHour ?? 4;
    document.getElementById("scanMinute").value = cfg.ScanMinute ?? 0;
    document.getElementById("pathPrefixes").value = toLines(cfg.PathPrefixes);
    document.getElementById("ignorePatterns").value = toLines(cfg.IgnorePatterns);
  }

  async function save() {
    const cfg = await ApiClient.getPluginConfiguration(pluginId);

    cfg.IncludeMovies = document.getElementById("includeMovies").checked;
    cfg.IncludeSeries = document.getElementById("includeSeries").checked;
    cfg.MinItems = parseInt(document.getElementById("minItems").value, 10) || 0;
    cfg.Prefix = document.getElementById("prefix").value.trim();
    cfg.Suffix = document.getElementById("suffix").value.trim();

    const hour = Math.min(23, Math.max(0, parseInt(document.getElementById("scanHour").value, 10) || 4));
    const minute = Math.min(59, Math.max(0, parseInt(document.getElementById("scanMinute").value, 10) || 0));
    cfg.ScanHour = hour;
    cfg.ScanMinute = minute;

    cfg.PathPrefixes = fromLines(document.getElementById("pathPrefixes").value);
    cfg.IgnorePatterns = fromLines(document.getElementById("ignorePatterns").value);

    const result = await ApiClient.updatePluginConfiguration(pluginId, cfg);
    Dashboard.processPluginConfigurationUpdateResult(result);

    if (result?.IsUpdated === false) {
      Dashboard.alert("Konfiguration konnte nicht gespeichert werden.");
    } else {
      Dashboard.alert("Gespeichert.");
    }
  }

  // Manuell scannen via ScheduledTask-API (kein eigener Controller nötig)
  async function manualScan(btn) {
    try {
      btn?.setAttribute("disabled", "disabled");
      btn?.classList.add("idleProcessing");

      const tasks = await ApiClient.getJSON(ApiClient.getUrl("ScheduledTasks"));
      const t = tasks.find(x =>
        x?.Key === "FolderCollections.DailyScan" ||
        (typeof x?.Name === "string" && x.Name.toLowerCase().includes("folder collections"))
      );
      if (!t?.Id) throw new Error("FolderCollections-Task nicht gefunden.");

      try {
        await ApiClient.fetchApi(`/ScheduledTasks/${t.Id}/Trigger`, { method: "POST" });
      } catch {
        await ApiClient.fetchApi(`/ScheduledTasks/Running/${t.Id}`, { method: "POST" });
      }

      Dashboard.alert("Manueller Scan gestartet!");
    } catch (err) {
      Dashboard.alert("Fehler beim Starten des Scans: " + (err?.message || err));
      console.error(err);
    } finally {
      btn?.removeAttribute("disabled");
      btn?.classList.remove("idleProcessing");
    }
  }

  // Einmalige Initialisierung: auf Form-Submit hören (unkaputtbar)
  function init() {
    const page = document.getElementById("folderCollectionsConfigPage");
    if (!page || page.dataset.initialized === "1") return;
    page.dataset.initialized = "1";

    const form = document.getElementById("fcForm");
    const cancelBtn = document.getElementById("fcCancel");
    const scanBtn = document.getElementById("fcManualScan");

    form?.addEventListener("submit", (ev) => {
      ev.preventDefault();
      save().catch(console.error);
    });

    cancelBtn?.addEventListener("click", (ev) => {
      ev.preventDefault();
      history.back();
    });

    scanBtn?.addEventListener("click", (ev) => {
      ev.preventDefault();
      manualScan(ev.currentTarget).catch(console.error);
    });

    load().catch(console.error);
  }

  // Robust für alle Jellyfin-Frontends
  document.addEventListener("DOMContentLoaded", init);
  document.addEventListener("viewshow", init);
  document.addEventListener("pageshow", init);
})();
