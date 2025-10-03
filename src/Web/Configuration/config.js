(() => {
  'use strict';

  // >>> DEINE GUID HIER EINTRAGEN <<<
  const pluginId = '00000000-0000-0000-0000-000000000000';

  const S = {
    page: '#FolderCollectionsConfigurationPage',
    form: '#fcForm',
    reload: '#fcReload',
    root: '#fcRoot',
    pattern: '#fcPattern',
    includeExt: '#fcIncludeExt',
    excludeFolders: '#fcExcludeFolders',
    recurse: '#fcRecurse',
    minItems: '#fcMinItems',
    nameTpl: '#fcNameTpl',
    createCollections: '#fcCreateCollections',
    updateExisting: '#fcUpdateExisting',
    libraryIds: '#fcLibraryIds',
    libraryIdsInfo: '#fcLibraryIdsInfo',
    enable: '#fcEnableScan',
    scanInterval: '#fcScanInterval',
    dryRun: '#fcDryRun',
    logLevel: '#fcLogLevel',
    testPath: '#fcTestPath',
    save: '#fcSave'
  };

  const DEFAULTS = {
    RootPath: '/media/filme',
    Pattern: '.*',
    IncludeExtensionsCsv: 'mkv,mp4,avi',
    ExcludeFoldersCsv: '',
    Recurse: true,
    MinItemsPerCollection: 2,
    CollectionNameTemplate: '{folderName}',
    CreateCollections: true,
    UpdateExisting: true,
    LibraryIds: [],
    EnableScan: false,
    ScanIntervalMinutes: 60,
    DryRun: false,
    LogLevel: 'Information',
    TestPath: ''
  };

  let _cfgCache = null;

  function log(...a){ try{ console.debug('[FolderCollections][config]', ...a);}catch{} }
  function show(){ try{ Dashboard.showLoadingMsg(); }catch{} }
  function hide(){ try{ Dashboard.hideLoadingMsg(); }catch{} }
  function alertMsg(msg){ try{ Dashboard.alert(msg);}catch{ window.alert(msg); } }

  function getEl(sel){ return document.querySelector(sel); }

  function getVal(sel, type='text') {
    const el = getEl(sel);
    if (!el) return undefined;
    if (type === 'checkbox') return !!el.checked;
    if (type === 'number')   return el.value === '' ? undefined : Number(el.value);
    if (type === 'select-multiple') {
      return Array.from(el.selectedOptions || []).map(o => o.value);
    }
    return el.value;
  }

  function setVal(sel, value, type='text') {
    const el = getEl(sel);
    if (!el) return;
    if (type === 'checkbox') { el.checked = !!value; return; }
    if (type === 'number')   { el.value = (value ?? ''); return; }
    if (type === 'select-multiple') {
      const vals = new Set(value || []);
      Array.from(el.options).forEach(o => { o.selected = vals.has(o.value); });
      return;
    }
    el.value = (value ?? '');
  }

  async function loadLibraries() {
    const info = getEl(S.libraryIdsInfo);
    try {
      // Versuche Jellyfin-Clientfunktion, falle auf Fetch zurück
      let libs = null;
      if (typeof ApiClient.getVirtualFolders === 'function') {
        libs = await ApiClient.getVirtualFolders();
      } else {
        // Fallback auf REST
        const url = ApiClient.getUrl('Library/VirtualFolders');
        const resp = await fetch(url, { headers: { 'X-Emby-Token': ApiClient._serverInfo?.AccessToken || '' } });
        if (!resp.ok) throw new Error('HTTP ' + resp.status);
        libs = await resp.json();
      }

      const sel = getEl(S.libraryIds);
      sel.innerHTML = '';
      (libs?.Items || libs || []).forEach(vf => {
        const opt = document.createElement('option');
        opt.value = vf.Id || vf.ItemId || vf.CollectionType || vf.Name;
        opt.textContent = vf.Name || vf.CollectionType || vf.Id;
        sel.appendChild(opt);
      });

      if (info) info.textContent = (sel.options.length ? 'Bibliotheken geladen' : 'Keine Bibliotheken gefunden');
    } catch (e) {
      log('Bibliotheken laden fehlgeschlagen:', e);
      const infoText = 'Bibliotheken konnten nicht geladen werden – Auswahl wird ausgeblendet.';
      if (info) info.textContent = infoText;
      const wrap = getEl(S.libraryIds)?.closest('.inputContainer');
      if (wrap) wrap.style.display = 'none';
    }
  }

  function applyToForm(cfg) {
    setVal(S.root, cfg.RootPath ?? DEFAULTS.RootPath);
    setVal(S.pattern, cfg.Pattern ?? DEFAULTS.Pattern);
    setVal(S.includeExt, cfg.IncludeExtensionsCsv ?? DEFAULTS.IncludeExtensionsCsv);
    setVal(S.excludeFolders, cfg.ExcludeFoldersCsv ?? DEFAULTS.ExcludeFoldersCsv);
    setVal(S.recurse, cfg.Recurse ?? DEFAULTS.Recurse, 'checkbox');
    setVal(S.minItems, cfg.MinItemsPerCollection ?? DEFAULTS.MinItemsPerCollection, 'number');
    setVal(S.nameTpl, cfg.CollectionNameTemplate ?? DEFAULTS.CollectionNameTemplate);
    setVal(S.createCollections, cfg.CreateCollections ?? DEFAULTS.CreateCollections, 'checkbox');
    setVal(S.updateExisting, cfg.UpdateExisting ?? DEFAULTS.UpdateExisting, 'checkbox');
    setVal(S.enable, cfg.EnableScan ?? DEFAULTS.EnableScan, 'checkbox');
    setVal(S.scanInterval, cfg.ScanIntervalMinutes ?? DEFAULTS.ScanIntervalMinutes, 'number');
    setVal(S.dryRun, cfg.DryRun ?? DEFAULTS.DryRun, 'checkbox');
    setVal(S.logLevel, cfg.LogLevel ?? DEFAULTS.LogLevel);
    setVal(S.testPath, cfg.TestPath ?? DEFAULTS.TestPath);
    setVal(S.libraryIds, cfg.LibraryIds ?? DEFAULTS.LibraryIds, 'select-multiple');
  }

  function readFromForm(cfgBase) {
    const cfg = Object.assign({}, cfgBase); // Unbekannte Felder behalten
    cfg.RootPath = getVal(S.root);
    cfg.Pattern = getVal(S.pattern);
    cfg.IncludeExtensionsCsv = getVal(S.includeExt);
    cfg.ExcludeFoldersCsv = getVal(S.excludeFolders);
    cfg.Recurse = getVal(S.recurse, 'checkbox');
    cfg.MinItemsPerCollection = getVal(S.minItems, 'number');
    cfg.CollectionNameTemplate = getVal(S.nameTpl);
    cfg.CreateCollections = getVal(S.createCollections, 'checkbox');
    cfg.UpdateExisting = getVal(S.updateExisting, 'checkbox');
    cfg.LibraryIds = getVal(S.libraryIds, 'select-multiple') || [];
    cfg.EnableScan = getVal(S.enable, 'checkbox');
    cfg.ScanIntervalMinutes = getVal(S.scanInterval, 'number');
    cfg.DryRun = getVal(S.dryRun, 'checkbox');
    cfg.LogLevel = getVal(S.logLevel);
    cfg.TestPath = getVal(S.testPath);
    return cfg;
  }

  async function loadConfig() {
    show();
    try {
      const cfg = await ApiClient.getPluginConfiguration(pluginId);
      _cfgCache = cfg || {};
      applyToForm(Object.assign({}, DEFAULTS, _cfgCache));
      log('Konfiguration geladen', _cfgCache);
    } catch (err) {
      console.error('[FolderCollections] Laden fehlgeschlagen:', err);
      alertMsg('Konfiguration konnte nicht geladen werden. Details in der Konsole.');
      // Fallback: nur Defaults anzeigen
      _cfgCache = {};
      applyToForm(DEFAULTS);
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
      Dashboard.processPluginConfigurationUpdateResult(result);
      _cfgCache = merged;
      log('Konfiguration gespeichert', merged);
    } catch (err) {
      console.error('[FolderCollections] Speichern fehlgeschlagen:', err);
      alertMsg('Konfiguration konnte nicht gespeichert werden. Details in der Konsole.');
    } finally {
      hide();
    }
  }

  function wire() {
    const page = getEl(S.page);
    if (!page) return;

    page.addEventListener('pageshow', async () => {
      await Promise.all([ loadLibraries(), loadConfig() ]);
    });

    const f = getEl(S.form);
    if (f) f.addEventListener('submit', saveConfig);

    const r = getEl(S.reload);
    if (r) r.addEventListener('click', loadConfig);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', wire);
  } else {
    wire();
  }
})();
