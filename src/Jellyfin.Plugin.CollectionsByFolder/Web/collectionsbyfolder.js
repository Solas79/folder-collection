define([], function () {
  'use strict';

  const pluginId = 'f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41';
  function $(v,s){return v.querySelector(s);}
  function setStatus(v,m){const el=$(v,'#cbf-status'); if(el) el.textContent=m; console.log('[CBF]',m);}
  function linesToList(t){return (t||'').replace(/\r\n?/g,'\n').split('\n').map(s=>s.trim()).filter(Boolean);}

  function loadConfig(v){
    if(!window.ApiClient?.getPluginConfiguration){ setStatus(v,'ApiClient nicht verfügbar'); return Promise.resolve(); }
    return ApiClient.getPluginConfiguration(pluginId).then(cfg=>{
      $(v,'#whitelist').value=(cfg.Whitelist||[]).join('\n');
      $(v,'#blacklist').value=(cfg.Blacklist||[]).join('\n');
      $(v,'#prefix').value=cfg.Prefix||'';
      $(v,'#suffix').value=cfg.Suffix||'';
      $(v,'#minfiles').value=String(cfg.MinFiles??0);
      setStatus(v,'Konfiguration geladen');
    }).catch(e=>setStatus(v,'Fehler beim Laden: '+(e?.message||e)));
  }

  function onSave(v,e){
    e?.preventDefault?.(); e?.stopPropagation?.(); e?.stopImmediatePropagation?.();
    setStatus(v,'Speichern…');
    const cfg={
      Whitelist: linesToList($(v,'#whitelist').value),
      Blacklist: linesToList($(v,'#blacklist').value),
      Prefix:    $(v,'#prefix').value||'',
      Suffix:    $(v,'#suffix').value||'',
      MinFiles:  parseInt($(v,'#minfiles').value||'0',10)||0,
      FolderPaths: linesToList($(v,'#whitelist').value)
    };
    ApiClient.updatePluginConfiguration(pluginId,cfg)
      .then(()=>setStatus(v,'Gespeichert ✔'))
      .catch(e=>setStatus(v,'Fehler: '+(e?.message||e)));
  }

  function onScan(v,e){
    e?.preventDefault?.(); e?.stopPropagation?.(); e?.stopImmediatePropagation?.();
    setStatus(v,'Scan gestartet…'); setTimeout(()=>setStatus(v,'Scan abgeschlossen ✔ (Demo)'),800);
  }

  return function(view){
    view.addEventListener('viewshow', function(){
      $(view,'#saveButton')?.addEventListener('click', e=>onSave(view,e), {capture:true});
      $(view,'#scanNowButton')?.addEventListener('click', e=>onScan(view,e), {capture:true});
      loadConfig(view); setStatus(view,'Bereit'); console.log('[CBF] Listener gebunden');
    });
  };
});
