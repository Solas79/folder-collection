(() => {
  'use strict';

  // GUID deines Plugins:
  const pluginId = '9f4f2c47-b3c5-4b13-9b1f-1c9a5c3b8d6a';

  const $ = sel => document.querySelector(sel);
  const S = {
    form:'#fcForm',
    include:'#fcIncludeFolders',
    exclude:'#fcExcludeFolders',
    prefix:'#fcNamePrefix',
    suffix:'#fcNameSuffix',
    minIt:'#fcMinItemsPerFolder',
    enable:'#fcEnableDailyScan',
    time:'#fcDailyTime',
    reload:'#fcReload',
    save:'#fcSave',
    scan:'#fcScanNow'
  };

  const DEFAULTS = {
    IncludeFolders: [],
    ExcludeFolders: [],
    CollectionNamePrefix: '',
    CollectionNameSuffix: '',
    MinItemsPerFolder: 2,
    EnableDailyScan: false,
    DailyScanTime: '03:00',
    // Back-Compat:
    DailyScanHour: 3,
    DailyScanMinute: 0
  };

  const alertx = (m) => { try { Dashboard?.alert(m); } catch { alert(m); } };
  const parseLines = (t) => (t||'').split(/\r?\n/).map(s=>s.trim()).filter(Boolean);
  const joinLines  = (a) => Array.isArray(a) ? a.join('\n') : '';

  function normalize(cIn){
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

  function applyToForm(c){
    $(S.include).value = joinLines(c.IncludeFolders);
    $(S.exclude).value = joinLines(c.ExcludeFolders);
    $(S.prefix).value  = c.CollectionNamePrefix || '';
    $(S.suffix).value  = c.CollectionNameSuffix || '';
    $(S.minIt).value   = String(c.MinItemsPerFolder ?? DEFAULTS.MinItemsPerFolder);
    $(S.enable).checked= !!c.EnableDailyScan;
    $(S.time).value    = (/^\d{2}:\d{2}$/.test(c.DailyScanTime||'')) ? c.DailyScanTime : DEFAULTS.DailyScanTime;
  }

  function readFromForm(base){
    const out = Object.assign({}, base || {});
    out.IncludeFolders       = parseLines($(S.include).value);
    out.ExcludeFolders       = parseLines($(S.exclude).value);
    out.CollectionNamePrefix = $(S.prefix).value || '';
    out.CollectionNameSuffix = $(S.suffix).value || '';
    const mi = Number($(S.minIt).value);
    out.MinItemsPerFolder    = Number.isFinite(mi) ? Math.max(0, Math.trunc(mi)) : DEFAULTS.MinItemsPerFolder;
    out.EnableDailyScan      = $(S.enable).checked;
    const t = ($(S.time).value || '').trim();
    if (/^\d{2}:\d{2}$/.test(t)) {
      out.DailyScanTime = t;
      const [h,m] = t.split(':').map(n=>parseInt(n,10));
      out.DailyScanHour = h; out.DailyScanMinute = m;
    }
    return out;
  }

  async function loadConfig(){
    const cfg = await ApiClient.getPluginConfiguration(pluginId);
    applyToForm(normalize(cfg));
  }

  async function saveConfig(ev){
    if (ev) ev.preventDefault();
    const current = await ApiClient.getPluginConfiguration(pluginId);
    const merged  = readFromForm(normalize(current));
    const result  = await ApiClient.updatePluginConfiguration(pluginId, merged);
    if (result?.Errors?.length) {
      alertx('Fehler: ' + result.Errors[0].Description);
    } else {
      alertx('Konfiguration gespeichert.');
    }
  }

  async function scanNow(){
    const token = ApiClient?._serverInfo?.AccessToken || '';
    const post  = async (path)=>fetch(ApiClient.getUrl(path), { method:'POST', headers:{ 'X-Emby-Token': token }});
    let r = await post('FolderCollections/ScanNow');
    if (!r.ok && r.status === 404) r = await post('FolderCollections/Scan');
    if (!r.ok) throw new Error('HTTP ' + r.status);
    alertx('Scan gestartet.');
  }

  function wire(){
    $(S.form)?.addEventListener('submit', saveConfig);
    $(S.reload)?.addEventListener('click', ()=>loadConfig().catch(e=>alertx('Laden fehlgeschlagen')));
    $(S.scan)?.addEventListener('click', ()=>scanNow().catch(e=>alertx('Scan fehlgeschlagen')));
  }

  document.addEventListener('DOMContentLoaded', () => {
    if (!window.ApiClient) { alert('jellyfin-apiclient konnte nicht geladen werden.'); return; }
    wire();
    loadConfig().catch(e => { console.error(e); alertx('Konfiguration konnte nicht geladen werden.'); });
  });
})();
