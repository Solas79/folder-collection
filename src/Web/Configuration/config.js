(() => {
  'use strict';

  const pluginId = '9f4f2c47-b3c5-4b13-9b1f-1c9a5c3b8d6a';

  const $ = (sel) => document.querySelector(sel);
  const S = {
    page: '#FolderCollectionsConfigurationPage',
    form: '#fcForm',
    include: '#fcIncludeFolders',
    exclude: '#fcExcludeFolders',
    prefix:  '#fcNamePrefix',
    suffix:  '#fcNameSuffix',
    minItems:'#fcMinItemsPerFolder',
    enableDaily:'#fcEnableDailyScan',
    dailyTime:'#fcDailyTime',
    save:   '#fcSave',
    reload: '#fcReload',
    scanNow:'#fcScanNow'
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

  const log = (...a)=>{ try{console.debug('[FolderCollections][config]',...a);}catch{} };
  const toast=(m)=>{ try{Dashboard.alert(m);}catch{alert(m);} };

  const parseLines=(t)=>(t||'').split(/\r?\n/).map(s=>s.trim()).filter(Boolean);
  const joinLines =(a)=>Array.isArray(a)?a.join('\n'):'';

  function normalize(cIn){
    const c = Object.assign({}, DEFAULTS, cIn||{});
    if ((!c.DailyScanTime || !/^\d{2}:\d{2}$/.test(c.DailyScanTime)) && typeof c.DailyScanHour==='number'){
      const h=String(c.DailyScanHour).padStart(2,'0');
      const m=String(c.DailyScanMinute||0).padStart(2,'0');
      c.DailyScanTime=`${h}:${m}`;
    }
    if(!Array.isArray(c.IncludeFolders)) c.IncludeFolders=[];
    if(!Array.isArray(c.ExcludeFolders)) c.ExcludeFolders=[];
    if(!Number.isFinite(c.MinItemsPerFolder)) c.MinItemsPerFolder=DEFAULTS.MinItemsPerFolder;
    return c;
  }

  function applyToForm(c){
    $(S.include).value = joinLines(c.IncludeFolders);
    $(S.exclude).value = joinLines(c.ExcludeFolders);
    $(S.prefix).value  = c.CollectionNamePrefix || '';
    $(S.suffix).value  = c.CollectionNameSuffix || '';
    $(S.minItems).value= String(c.MinItemsPerFolder ?? DEFAULTS.MinItemsPerFolder);
    $(S.enableDaily).checked = !!c.EnableDailyScan;
    $(S.dailyTime).value = (/^\d{2}:\d{2}$/.test(c.DailyScanTime||'')) ? c.DailyScanTime : DEFAULTS.DailyScanTime;
  }

  function readFromForm(base){
    const out = Object.assign({}, base||{});
    out.IncludeFolders = parseLines($(S.include).value);
    out.ExcludeFolders = parseLines($(S.exclude).value);
    out.CollectionNamePrefix = $(S.prefix).value || '';
    out.CollectionNameSuffix = $(S.suffix).value || '';
    const mi = Number($(S.minItems).value);
    out.MinItemsPerFolder = Number.isFinite(mi) ? Math.max(0, Math.trunc(mi)) : DEFAULTS.MinItemsPerFolder;
    out.EnableDailyScan = $(S.enableDaily).checked;
    const t = ($(S.dailyTime).value||'').trim();
    if (/^\d{2}:\d{2}$/.test(t)){
      out.DailyScanTime=t;
      const [h,m]=t.split(':').map(n=>parseInt(n,10));
      out.DailyScanHour=h; out.DailyScanMinute=m;
    }
    return out;
  }

  async function loadConfig(){
    try{
      const serverCfg = await ApiClient.getPluginConfiguration(pluginId);
      applyToForm(normalize(serverCfg));
      log('Konfiguration geladen.');
    }catch(e){
      console.error('[FolderCollections] Laden fehlgeschlagen:', e);
      applyToForm(normalize(null));
      toast('Konfiguration konnte nicht geladen werden. Defaults angezeigt.');
    }
  }

  async function saveConfig(ev){
    if(ev) ev.preventDefault();
    try{
      const current = await ApiClient.getPluginConfiguration(pluginId);
      const merged = readFromForm(normalize(current));
      const result = await ApiClient.updatePluginConfiguration(pluginId, merged);
      try { Dashboard.processPluginConfigurationUpdateResult(result); } catch {}
      toast('Konfiguration gespeichert.');
      log('Gespeichert:', merged);
    }catch(e){
      console.error('[FolderCollections] Speichern fehlgeschlagen:', e);
      toast('Speichern fehlgeschlagen – siehe Konsole.');
    }
  }

  async function scanNow(){
    try{
      const token = ApiClient._serverInfo?.AccessToken || '';
      const post = async (path)=>fetch(ApiClient.getUrl(path),{method:'POST',headers:{'X-Emby-Token':token}});
      let r = await post('FolderCollections/ScanNow');
      if(!r.ok && r.status===404) r = await post('FolderCollections/Scan');
      if(!r.ok) throw new Error('HTTP '+r.status);
      toast('Scan gestartet.');
    }catch(e){
      console.error('[FolderCollections] ScanNow fehlgeschlagen:', e);
      toast('Scan konnte nicht gestartet werden – siehe Konsole.');
    }
  }

  function wire(){
    const page = $(S.page);
    if(!page){ console.error('Config-Page nicht gefunden'); return; }

    // Beide Events unterstützen (manche Jellyfin-Versionen feuern nur eins):
    const onShow = ()=>loadConfig();
    page.addEventListener('pageshow', onShow);
    page.addEventListener('viewshow', onShow);

    $(S.form)?.addEventListener('submit', saveConfig);
    $(S.reload)?.addEventListener('click', loadConfig);
    $(S.scanNow)?.addEventListener('click', scanNow);
  }

  if(document.readyState==='loading'){
    document.addEventListener('DOMContentLoaded', wire);
  }else{
    wire();
  }
})();
