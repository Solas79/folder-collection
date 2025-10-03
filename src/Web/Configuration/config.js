(() => {
  'use strict';

  const pluginId = '9f4f2c47-b3c5-4b13-9b1f-1c9a5c3b8d6a';

  const $ = (sel) => document.querySelector(sel);
  const S = {
    page: '#FolderCollectionsConfigurationPage',
    form: '#fcForm',
    include: '#fcIncludeFolders',
    exclude: '#fcExcludeFolders',
    prefix: '#fcNamePrefix',
    suffix: '#fcNameSuffix',
    minItemsPerFolder: '#fcMinItemsPerFolder',
    enableDaily: '#fcEnableDailyScan',
    dailyTime: '#fcDailyTime',
    save: '#fcSave',
    reload: '#fcReload',
    scanNow: '#fcScanNow'
  };

  const DEFAULTS = {
    IncludeFolders: [],
    ExcludeFolders: [],
    CollectionNamePrefix: '',
    CollectionNameSuffix: '',
    MinItemsPerFolder: 2,
    EnableDailyScan: false,
    DailyScanTime: '03:00',
    DailyScanHour: 3,
    DailyScanMinute: 0
  };

  let _wired = false;

  const log  = (...a) => { try { console.debug('[FolderCollections][config]', ...a);} catch {} };
  const show = () => { try { Dashboard.showLoadingMsg(); } catch {} };
  const hide = () => { try { Dashboard.hideLoadingMsg(); } catch {} };
  const toast= (m) => { try { Dashboard.alert(m); } catch { alert(m); } };

  const parseLines = (t) => (t||'').split(/\r?\n/).map(s=>s.trim()).filter(Boolean);
  const joinLines  = (a) => Array.isArray(a) ? a.join('\n') : '';

  function normalize(cIn) {
    const c = Object.assign({}, DEFAULTS, cIn || {});
    if ((!c.DailyScanTime || !/^\d{2}:\d{2}$/.test(c.DailyScanTime)) && typeof c.DailyScanHour === 'number') {
      const h = String(c.DailyScanHour).padStart(2,'0');
      const m = String(c.DailyScanMinute || 0).padStart(2,'0');
      c.DailyScanTime = `${h}:${m}`;
    }
    if (!Array.isArray(c.IncludeFolders)) c.IncludeFolders = [];
    if (!Array.isArray(c.ExcludeFolders)) c.ExcludeFolders = [];
    if (!Number.isFinite(c.MinItemsPerFolder)) c.MinItemsPerFolder = DEFAULTS.MinItemsPerFolder;
    return c;
  }

  function applyToForm(c) {
    $(S.include).value = joinLines(c.IncludeFolders);
    $(S.exclude).value = joinLines(c.ExcludeFolders);
    $(S.prefix).value  = c.CollectionNamePrefix || '';
    $(S.suffix).value  = c.CollectionNameSuffix || '';
    $(S.minItemsPerFolder).value = String(c.MinItemsPerFolder ?? DEFAULTS.MinItemsPerFolder);
    $(S.enableDaily).checked = !!c.EnableDailyScan;
    $(S.dailyTime).value = (/^\d{2}:\d{2}$/.test(c.DailyScanTime||'')) ? c.DailyScanTime : DEFAULTS.DailyScanTime;
  }

  function readFromForm(base) {
    const out = Object.assign({}, base || {});
    out.IncludeFolders = parseLines($(S.include).value);
    out.ExcludeFolders = parseLines($(S.exclude).value);
    out.CollectionNamePrefix = $(S.prefix).value || '';
    out.CollectionNameSuffix = $(S.suffix).value || '';
    const mi = Number($(S.minItemsPerFolder).value);
    out.MinItemsPerFolder = Number.isFinite(mi) ? Math.max(0, Math.trunc(mi)) : DEFAULTS.MinItemsPerFolder;
    out.EnableDailyScan = $(S.enableDaily).checked;
    const t = ($(S.dailyTime).value || '').trim();
    if (/^\d{2}:\d{2}$/.test(t)) {
      out.DailyScanTime = t;
      const [h, m] = t.split(':').map(n => parseInt(n,10));
      out.DailyScanHour = h;
      out.DailyScanMinute = m;
    }
    return out;
  }

  async function loadConfig() {
    // Wichtig: Spinner nicht hängen lassen, selbst bei Exception.
    show();
    try {
      const serverCfg = await ApiClient.getPluginConfiguration(pluginId);
      const cfg = normalize(serverCfg);
      applyToForm(cfg);
      log('Konfiguration geladen:', cfg);
    } catch (e) {
      console.error('[FolderCollections] Laden fehlgeschlagen:', e);
      applyToForm(normalize(null));
      toast('Konfiguration konnte nicht geladen werden. Defaults angezeigt.');
    } finally {
      hide(); // <- Overlay immer schließen
    }
  }

  async function saveConfig(ev) {
    if (ev) ev.preventDefault();
    show();
    try {
      const current = await ApiClient.getPluginConfiguration(pluginId);
      const merged = readFromForm(normalize(current));
      const result = await ApiClient.updatePluginConfiguration(pluginId, merged);
      try { Dashboard.processPluginConfigurationUpdateResult(result); } catch {}
      toast('Konfiguration gespeichert.');
      log('Konfiguration gespeichert', merged);
    } catch (e) {
      console.error('[FolderCollections] Speichern fehlgeschlagen:', e);
      toast('Speichern fehlgeschlagen – siehe Konsole.');
    } finally {
      hide();
    }
  }

  async function scanNow() {
    show();
    try {
      const token = ApiClient._serverInfo?.AccessToken || '';
      const post = async (path) => fetch(ApiClient.getUrl(path), { method:'POST', headers:{ 'X-Emby-Token': token }});
      let r = await post('FolderCollections/ScanNow');
      if (!r.ok && r.status === 404) r = await post('FolderCollections/Scan');
      if (!r.ok) throw new Error('HTTP ' + r.status);
      toast('Scan gestartet.');
    } catch (e) {
      console.error('[FolderCollections] ScanNow fehlgeschlagen:', e);
      toast('Scan konnte nicht gestartet werden – siehe Konsole.');
    } finally {
      hide();
    }
  }

  function wire() {
    if (_wired) return;
    _wired = true;

    const page = $(S.page);
    if (!page) {
      console.error('[FolderCollections] Seite nicht gefunden – IDs prüfen!');
      return;
    }

    const onShow = () => { log('view/pageshow'); loadConfig(); };
    page.addEventListener('pageshow', onShow);
    page.addEventListener('viewshow', onShow);

    $(S.form)?.addEventListener('submit', saveConfig);
    $(S.reload)?.addEventListener('click', loadConfig);
    $(S.scanNow)?.addEventListener('click', scanNow);

    // Kleine Klick-Logs zur Diagnose
    $(S.save)?.addEventListener('click', () => log('Save click'));
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', wire);
  } else {
    wire();
  }
})();
