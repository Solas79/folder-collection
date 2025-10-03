(() => {
  'use strict';

  // >>> DEINE GUID HIER EINFÜGEN <<<
  const pluginId = '00000000-0000-0000-0000-000000000000';

  const S = {
    page: '#FolderCollectionsConfigurationPage',
    form: '#fcForm',
    include: '#fcIncludeFolders',
    exclude: '#fcExcludeFolders',
    prefix: '#fcNamePrefix',
    suffix: '#fcNameSuffix',
    useBaseNameName:   '#fcUseBasenameAsCollectionName',
    useBaseNameFolder: '#fcUseBasenameAsCollectionFolder',
    enableDaily: '#fcEnableDailyScan',
    dailyTime:   '#fcDailyTime',
    save:   '#fcSave',
    reload: '#fcReload',
    scanNow: '#fcScanNow'
  };

  const DEFAULTS = {
    IncludeFolders: [],
    ExcludeFolders: [],
    CollectionNamePrefix: '',
    CollectionNameSuffix: '',
    UseFolderBasenameAsCollectionName: true,
    UseFolderBasenameAsCollectionFolder: false,
    EnableDailyScan: false,
    DailyScanTime: '03:00' // HH:mm
    // Optional: Backwards-Compat: DailyScanHour / DailyScanMinute werden unterstützt (s.u.)
  };

  let _cfgCache = null;
  let _initDone = false;

  function log(...a){ try{ console.debug('[FolderCollections][config]', ...a);}catch{} }
  function show(){ try{ Dashboard.showLoadingMsg(); }catch{} }
  function hide(){ try{ Dashboard.hideLoadingMsg(); }catch{} }
  function toast(msg){ try{ Dashboard.alert(msg); }catch{ alert(msg); } }

  function $(sel){ return document.querySelector(sel); }

  function parseLines(text) {
    return (text || '')
      .split(/\r?\n/g)
      .map(s => s.trim())
      .filter(s => s.length > 0);
  }
  function joinLines(arr) {
    return (arr && Array.isArray(arr) ? arr : []).join('\n');
  }

  function cfgWithDefaults(c) {
    const cfg = Object.assign({}, DEFAULTS, c || {});
    // Backwards-Compat: falls noch getrennte Hour/Minute vorhanden sind
    if (!cfg.DailyScanTime && (typeof cfg.DailyScanHour === 'number')) {
      const h = String(cfg.DailyScanHour).padStart(2, '0');
      const m = String(cfg.DailyScanMinute || 0).padStart(2, '0');
      cfg.DailyScanTime = `${h}:${m}`;
    }
    return cfg;
  }

  function applyToForm(cfg) {
    $(S.include).value = joinLines(cfg.IncludeFolders);
    $(S.exclude).value = joinLines(cfg.ExcludeFolders);
    $(S.prefix).value  = cfg.CollectionNamePrefix || '';
    $(S.suffix).value  = cfg.CollectionNameSuffix || '';
    $(S.useBaseNameName).checked   = !!cfg.UseFolderBasenameAsCollectionName;
    $(S.useBaseNameFolder).checked = !!cfg.UseFolderBasenameAsCollectionFolder;
    $(S.enableDaily).checked = !!cfg.EnableDailyScan;

    // daily time als HH:mm
    const t = (cfg.DailyScanTime && /^\d{2}:\d{2}$/.test(cfg.DailyScanTime)) ? cfg.DailyScanTime : DEFAULTS.DailyScanTime;
    $(S.dailyTime).value = t;
  }

  function readFromForm(base) {
    const out = Object.assign({}, base || {});
    out.IncludeFolders = parseLines($(S.include).value);
    out.ExcludeFolders = parseLines($(S.exclude).value);
    out.CollectionNamePrefix = $(S.prefix).value || '';
    out.CollectionNameSuffix = $(S.suffix).value || '';
    out.UseFolderBasenameAsCollectionName = $(S.useBaseNameName).checked;
    out.UseFolderBasenameAsCollectionFolder = $(S.useBaseNameFolder).checked;
    out.EnableDailyScan = $(S.enableDaily).checked;

    const timeVal = ($(S.dailyTime).value || '').trim();
    if (/^\d{2}:\d{2}$/.test(timeVal)) {
      out.DailyScanTime = timeVal;
      // Für ältere Server, die Hour/Minute lesen:
      const [h, m] = timeVal.split(':').map(n => parseInt(n, 10));
      out.DailyScanHour = h;
      out.DailyScanMinute = m;
    }

    return out;
  }

  async function loadConfig() {
    show();
    try {
      const serverCfg = await ApiClient.getPluginConfiguration(pluginId);
      _cfgCache = cfgWithDefaults(serverCfg);
      applyToForm(_cfgCache);
      log('Konfiguration geladen:', _cfgCache);
    } catch (e) {
      console.error('[FolderCollections] Laden fehlgeschlagen:', e);
      _cfgCache = cfgWithDefaults(null);
      applyToForm(_cfgCache);
      toast('Konfiguration konnte nicht geladen werden (siehe Konsole). Defaults angezeigt.');
    } finally {
      hide();
    }
  }

  async function saveConfig(ev) {
    if (ev) ev.preventDefault();
    show();
    try {
      const current = await ApiClient.getPluginConfiguration(pluginId);
      const merged = readFromForm(current || {});
      const result = await ApiClient.updatePluginConfiguration(pluginId, merged);
      // Im Erfolgsfall kann result leer sein; Jellyfin-Helfer verarbeitet das robust:
      try { Dashboard.processPluginConfigurationUpdateResult(result); } catch {}
      _cfgCache = merged;
      log('Konfiguration gespeichert:', merged);
      toast('Konfiguration gespeichert.');
    } catch (e) {
      console.error('[FolderCollections] Speichern fehlgeschlagen:', e);
      toast('Speichern fehlgeschlagen (siehe Konsole).');
    } finally {
      hide();
    }
  }

  async function scanNow() {
    show();
    try {
      // 1) Bevorzugter Plugin-Endpunkt (falls von dir implementiert)
      const token = ApiClient._serverInfo?.AccessToken || '';
      const tryPost = async (path) => {
        const url = ApiClient.getUrl(path);
        const resp = await fetch(url, { method: 'POST', headers: { 'X-Emby-Token': token } });
        return resp;
      };

      // erst /FolderCollections/ScanNow, dann /FolderCollections/Scan als Fallback
      let resp = await tryPost('FolderCollections/ScanNow');
      if (!resp.ok && resp.status === 404) {
        resp = await tryPost('FolderCollections/Scan');
      }
      if (!resp.ok) throw new Error(`HTTP ${resp.status}`);

      toast('Scan gestartet.');
    } catch (e) {
      console.error('[FolderCollections] ScanNow fehlgeschlagen:', e);
      toast('Scan konnte nicht gestartet werden (siehe Konsole).');
    } finally {
      hide();
    }
  }

  function wire() {
    if (_initDone) return;
    _initDone = true;

    const page = $(S.page);
    if (!page) return;

    // In Jellyfin funktionieren je nach Version "pageshow" und teils "viewshow".
    const onShow = () => loadConfig();
    page.addEventListener('pageshow', onShow);
    page.addEventListener('viewshow', onShow);

    const form = $(S.form);
    if (form) form.addEventListener('submit', saveConfig);
    const btnReload = $(S.reload);
    if (btnReload) btnReload.addEventListener('click', loadConfig);
    const btnScan = $(S.scanNow);
    if (btnScan) btnScan.addEventListener('click', scanNow);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', wire);
  } else {
    wire();
  }
})();
