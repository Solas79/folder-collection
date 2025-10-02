(() => {
  const pluginId = "9f4f2c47-b3c5-4b13-9b1f-1c9a5c3b8d6a";

  const $ = (id) => document.getElementById(id);
  const toLines   = (arr)  => Array.isArray(arr) ? arr.join("\n") : (arr || "");
  const fromLines = (text) => (text || "").split("\n").map(s => s.trim()).filter(Boolean);

  // ---------- Fehler-Helfer ----------
  function buildErrorMessage(err) {
    try {
      if (err == null) return "Unbekannter Fehler";
      if (typeof err === "string") return err;

      const status     = typeof err.status !== "undefined" ? err.status : null;
      const statusText = typeof err.statusText === "string" ? err.statusText : "";
      const message    = typeof err.message === "string" ? err.message : "";

      if (status || statusText) {
        const head = status ? `HTTP ${status}${statusText ? " " : ""}${statusText}` : statusText;
        return message ? `${head} – ${message}` : head;
      }
      if (message) return message;

      return JSON.stringify(err);
    } catch {
      return "Unbekannter Fehler";
    }
  }

  async function safeReadBody(resp) {
    try {
      if (!resp || typeof resp.text !== "function") return "";
      const t = await resp.text();
      return (t || "").slice(0, 200);
    } catch { return ""; }
  }

  // ---------- Laden ----------
  async function load() {
    try {
      const cfg = await ApiClient.getPluginConfiguration(pluginId);

      $("includeMovies").checked = !!cfg.IncludeMovies;
      $("includeSeries").checked = !!cfg.IncludeSeries;
      $("minItems").value  = cfg.MinItems  ?? 2;
      $("prefix").value    = cfg.Prefix    ?? "";
      $("suffix").value    = cfg.Suffix    ?? "";
      $("scanHour").value  = cfg.ScanHour  ?? 4;
      $("scanMinute").value= cfg.ScanMinute?? 0;
      $("pathPrefixes").value  = toLines(cfg.PathPrefixes);
      $("ignorePatterns").value= toLines(cfg.IgnorePatterns);

      if ($("useBasename")) {
        $("useBasename").checked = !!cfg.UseBasenameAsCollectionName;
      }
    } catch (err) {
      Dashboard.alert("Konfiguration konnte nicht geladen werden: " + buildErrorMessage(err));
      console.error(err);
    }
  }

  // ---------- Speichern (mit Roundtrip-Verify) ----------
  async function save() {
    try {
      const cfg = await ApiClient.getPluginConfiguration(pluginId);

      cfg.IncludeMovies = $("includeMovies").checked;
      cfg.IncludeSeries = $("includeSeries").checked;
      cfg.MinItems = parseInt($("minItems").value, 10) || 0;
      cfg.Prefix   = $("prefix").value.trim();
      cfg.Suffix   = $("suffix").value.trim();

      const hour   = Math.min(23, Math.max(0, parseInt($("scanHour").value, 10)   || 4));
      const minute = Math.min(59, Math.max(0, parseInt($("scanMinute").value, 10) || 0));
      cfg.ScanHour   = hour;
      cfg.ScanMinute = minute;

      cfg.PathPrefixes  = fromLines($("pathPrefixes").value);
      cfg.IgnorePatterns= fromLines($("ignorePatterns").value);

      if ($("useBasename")) {
        cfg.UseBasenameAsCollectionName = !!$("useBasename").checked;
      }

      const result = await ApiClient.updatePluginConfiguration(pluginId, cfg);
      Dashboard.processPluginConfigurationUpdateResult(result);

      // Roundtrip-Verify: direkt wieder laden & prüfen
      const verify = await ApiClient.getPluginConfiguration(pluginId);
      const want   = !!$("useBasename")?.checked;
      const got    = !!verify.UseBasenameAsCollectionName;

      if (result?.IsUpdated === false) {
        Dashboard.alert("Konfiguration konnte nicht gespeichert werden.");
      } else if ($("useBasename") && want !== got) {
        Dashboard.alert("Hinweis: 'Basename als Sammlungsname' wurde serverseitig nicht übernommen. Prüfe PluginConfiguration.");
        console.warn("Roundtrip mismatch: wanted", want, "got", got, verify);
      } else {
        Dashboard.alert("Gespeichert.");
      }
    } catch (err) {
      Dashboard.alert("Speichern fehlgeschlagen: " + buildErrorMessage(err));
      console.error(err);
    }
  }

  // ---------- Manueller Scan ----------
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

      let resp = await ApiClient.fetchApi(`/ScheduledTasks/${t.Id}/Trigger`, { method: "POST" });
      if (!resp?.ok) {
        resp = await ApiClient.fetchApi(`/ScheduledTasks/Running/${t.Id}`, { method: "POST" });
        if (!resp?.ok) {
          const txt = await safeReadBody(resp);
          throw new Error(`${resp?.status || ""} ${resp?.statusText || ""} ${txt}`.trim());
        }
      }

      Dashboard.alert("Manueller Scan gestartet!");
    } catch (err) {
      Dashboard.alert("Fehler beim Starten des Scans: " + buildErrorMessage(err));
      console.error(err);
    } finally {
      btn?.removeAttribute("disabled");
      btn?.classList.remove("idleProcessing");
    }
  }

  // ---------- Init ----------
  function init() {
    const page = $("folderCollectionsConfigPage");
    if (!page || page.dataset.initialized === "1") return;
    page.dataset.initialized = "1";

    $("fcForm")?.addEventListener("submit", (ev) => {
      ev.preventDefault();
      save().catch((err) => {
        Dashboard.alert("Speichern fehlgeschlagen: " + buildErrorMessage(err));
        console.error(err);
      });
    });

    $("fcCancel")?.addEventListener("click", (ev) => {
      ev.preventDefault();
      history.back();
    });

    $("fcManualScan")?.addEventListener("click", (ev) => {
      ev.preventDefault();
      manualScan(ev.currentTarget).catch((err) => {
        Dashboard.alert("Fehler beim Starten des Scans: " + buildErrorMessage(err));
        console.error(err);
      });
    });

    load().catch((err) => {
      Dashboard.alert("Konfiguration konnte nicht geladen werden: " + buildErrorMessage(err));
      console.error(err);
    });
  }

  document.addEventListener("DOMContentLoaded", init);
  document.addEventListener("viewshow", init);
  document.addEventListener("pageshow", init);
})();
