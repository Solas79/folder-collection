(() => {
  // GUID exakt wie in Plugin.cs & manifest.json
  const pluginId = "9f4f2c47-b3c5-4b13-9b1f-1c9a5c3b8d6a";

  function toLines(arr) { return Array.isArray(arr) ? arr.join("\n") : (arr || ""); }
  function fromLines(text) { return (text || "").split("\n").map(s => s.trim()).filter(Boolean); }

  function getEl(id) { return document.getElementById(id); }

  async function load() {
    const cfg = await ApiClient.getPluginConfiguration(pluginId);

    getEl("includeMovies").checked = !!cfg.IncludeMovies;
    getEl("includeSeries").checked = !!cfg.IncludeSeries;
    getEl("minItems").value = cfg.MinItems ?? 2;
    getEl("prefix").value = cfg.Prefix ?? "";
    getEl("suffix").value = cfg.Suffix ?? "";
    getEl("scanHour").value = cfg.ScanHour ?? 4;
    getEl("scanMinute").value = cfg.ScanMinute ?? 0;
    getEl("pathPrefixes").value = toLines(cfg.PathPrefixes);
    getEl("ignorePatterns").value = toLines(cfg.IgnorePatterns);

    // NEU: Checkbox für "Basename als Sammlungsname"
    if (getEl("useBasename")) {
      getEl("useBasename").checked = !!cfg.UseBasenameAsCollectionName;
    }
  }

  async function save() {
    const cfg = await ApiClient.getPluginConfiguration(pluginId);

    cfg.IncludeMovies = getEl("includeMovies").checked;
    cfg.IncludeSeries = getEl("includeSeries").checked;
    cfg.MinItems = parseInt(getEl("minItems").value, 10) || 0;
    cfg.Prefix = getEl("prefix").value.trim();
    cfg.Suffix = getEl("suffix").value.trim();

    const hour = Math.min(23, Math.max(0, parseInt(getEl("scanHour").value, 10) || 4));
    const minute = Math.min(59, Math.max(0, parseInt(getEl("scanMinute").value, 10) || 0));
    cfg.ScanHour = hour;
    cfg.ScanMinute = minute;

    cfg.PathPrefixes = fromLines(getEl("pathPrefixes").value);
    cfg.IgnorePatterns = fromLines(getEl("ignorePatterns").value);

    // NEU: Checkbox speichern
    if (getEl("useBasename")) {
      cfg.UseBasenameAsCollectionName = getEl("useBasename").checked;
    }

    try {
      const result = await ApiClient.updatePluginConfiguration(pluginId, cfg);
      Dashboard.processPluginConfigurationUpdateResult(result);
      if (result?.IsUpdated === false) {
        Dashboard.alert("Konfiguration konnte nicht gespeichert werden.");
      } else {
        Dashboard.alert("Gespeichert.");
      }
    } catch (err) {
      const msg = (err && (err.statusText || err.message)) || "Unbekannter Fehler";
      Dashboard.alert("Speichern fehlgeschlagen: " + msg);
      console.error(err);
    }
  }

  // Manuell scannen via ScheduledTask-API (robust & saubere Errors)
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

      // Erst regulär triggern, bei 409/konkurrierend auf Running-Fallback
      let resp = await ApiClient.fetchApi(`/ScheduledTasks/${t.Id}/Trigger`, { method: "POST" });
      if (!resp?.ok) {
        // Fallback: evtl. Endpunkt-Variation (wie bei älteren Servern)
        resp = await ApiClient.fetchApi(`/ScheduledTasks/Running/${t.Id}`, { method: "POST" });
        if (!resp?.ok) {
          const txt = await safeReadBody(resp);
          throw new Error(`${resp.status} ${resp.statusText || ""} ${txt}`.trim());
        }
      }

      Dashboard.alert("Manueller Scan gestartet!");
    } catch (err) {
      const msg = buildErrorMessage(err);
      Dashboard.alert("Fehler beim Starten des Scans: " + msg);
      console.error(err);
    } finally {
      btn?.removeAttribute("disabled");
      btn?.classList.remove("idleProcessing");
    }
  }

  // Hilfsfunktionen für robuste Fehlertexte
  function buildErrorMessage(err) {
    // ApiClient.fetchApi gibt bei Fehlern teils Response-ähnliche Objekte, teils Exceptions
    if (!err) return "Unbekannter Fehler";
    // Versuch: Status/StatusText
    const statusTxt = err.statusText || err.status;
    const msg = err.message || "";
    if (statusTxt && msg) return `${statusTxt}: ${msg}`;
    return statusTxt || msg || String(err);
  }

  async function safeReadBody(resp) {
    try {
      if (!resp || typeof resp.text !== "function") return "";
      const t = await resp.text();
      return (t || "").slice(0, 200); // nicht zu lang machen
    } catch { return ""; }
  }

  // Einmalige Initialisierung: auf Form-Submit hören (unkaputtbar)
  function init() {
    const page = getEl("folderCollectionsConfigPage");
    if (!page || page.dataset.initialized === "1") return;
    page.dataset.initialized = "1";

    const form = getEl("fcForm");
    const cancelBtn = getEl("fcCancel");
    const scanBtn = getEl("fcManualScan");

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
