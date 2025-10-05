define([], function () {
  'use strict';
  const pluginId = 'f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41';

  function setStatus(view, msg) {
    const el = view.querySelector('#cbf-status');
    if (el) el.textContent = msg;
    console.log('[CBF]', msg);
  }

  function linesToList(t) {
    return (t || '').replace(/\r\n?/g, '\n').split('\n').map(s=>s.trim()).filter(Boolean);
  }

  function loadConfig(view) {
    if (!window.ApiClient?.getPluginConfiguration) { setStatus(view,'ApiClient nicht verfügbar'); return Promise.resolve(); }
    return ApiClient.getPluginConfiguration(pluginId).then(cfg => {
      view.querySelector('#whitelist').value = (cfg.Whitelist || []).join('\n');
      view.querySelector('#blacklist').value = (cfg.Blacklist || []).join('\n');
      view.querySelector('#prefix').value    = cfg.Prefix || '';
      view.querySelector('#suffix').value    = cfg.Suffix || '';
      view.querySelector('#minfiles').value  = String(cfg.MinFiles ?? 0);
      setStatus(view, 'Konfiguration geladen');
    }).catch(err => setStatus(view, 'Fehler beim Laden: ' + (err?.message || err)));
  }

  function onSave(view, e) {
    e?.preventDefault?.(); e?.stopPropagation?.(); e?.stopImmediatePropagation?.();
    setStatus(view, 'Speichern…');
    const cfg = {
      Whitelist:  linesToList(view.querySelector('#whitelist').value),
      Blacklist:  linesToList(view.querySelector('#blacklist').value),
      Prefix:     view.querySelector('#prefix').value || '',
      Suffix:     view.querySelector('#suffix').value || '',
      MinFiles:   parseInt(view.querySelector('#minfiles').value || '0', 10) || 0,
      FolderPaths: linesToList(view.querySelector('#whitelist').value)
    };
    ApiClient.updatePluginConfiguration(pluginId, cfg)
      .then(() => setStatus(view, 'Gespeichert ✔'))
      .catch(err => setStatus(view, 'Fehler: ' + (err?.message || err)));
  }

  function onScan(view, e) {
    e?.preventDefault?.(); e?.stopPropagation?.(); e?.stopImmediatePropagation?.();
    setStatus(view, 'Scan gestartet…');
    setTimeout(() => setStatus(view, 'Scan abgeschlossen ✔ (Demo)'), 800);
  }

  // Jellyfin ruft dieses Init mit der View auf
  return function (view) {
    view.addEventListener('viewshow', function () {
      view.querySelector('#saveButton')?.addEventListener('click', (e)=>onSave(view,e), {capture:true});
      view.querySelector('#scanNowButton')?.addEventListener('click', (e)=>onScan(view,e), {capture:true});
      loadConfig(view);
      setStatus(view, 'Bereit');
      console.log('[CBF] Listener gebunden');
    });
  };
});
