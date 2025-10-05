define([], function () {
  'use strict';

  const pluginId = 'f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41';

  function $(view, sel) { return view.querySelector(sel); }
  function setStatus(view, msg) {
    const el = $(view, '#cbf-status');
    if (el) el.textContent = msg;
    console.log('[CBF]', msg);
  }
  function linesToList(text) {
    return (text || '').replace(/\r\n?/g, '\n').split('\n').map(s => s.trim()).filter(Boolean);
  }

  function loadConfig(view) {
    if (!window.ApiClient?.getPluginConfiguration) {
      setStatus(view, 'ApiClient nicht verfügbar');
      return Promise.resolve();
    }
    return ApiClient.getPluginConfiguration(pluginId).then(cfg => {
      $(view, '#whitelist').value = (cfg.Whitelist || []).join('\n');
      $(view, '#blacklist').value = (cfg.Blacklist || []).join('\n');
      $(view, '#prefix').value    = cfg.Prefix || '';
      $(view, '#suffix').value    = cfg.Suffix || '';
      $(view, '#minfiles').value  = String(cfg.MinFiles ?? 0);
      setStatus(view, 'Konfiguration geladen');
    }).catch(err => setStatus(view, 'Fehler beim Laden: ' + (err?.message || err)));
  }

  function onSave(view, e) {
    e?.preventDefault?.(); e?.stopPropagation?.(); e?.stopImmediatePropagation?.();
    setStatus(view, 'Speichern…');
    const cfg = {
      Whitelist:   linesToList($(view, '#whitelist').value),
      Blacklist:   linesToList($(view, '#blacklist').value),
      Prefix:      $(view, '#prefix').value || '',
      Suffix:      $(view, '#suffix').value || '',
      MinFiles:    parseInt($(view, '#minfiles').value || '0', 10) || 0,
      // Kompatibilität für vorhandenen Code:
      FolderPaths: linesToList($(view, '#whitelist').value)
    };
    return ApiClient.updatePluginConfiguration(pluginId, cfg)
      .then(() => setStatus(view, 'Gespeichert ✔'))
      .catch(err => setStatus(view, 'Fehler: ' + (err?.message || err)));
  }

  function onScan(view, e) {
    e?.preventDefault?.(); e?.stopPropagation?.(); e?.stopImmediatePropagation?.();
    setStatus(view, 'Scan gestartet…');
    setTimeout(() => setStatus(view, 'Scan abgeschlossen ✔ (Demo)'), 800);
  }

  // Jellyfin ruft dieses Init mit der gerenderten View auf
  return function (view) {
    view.addEventListener('viewshow', function () {
      $(view, '#saveButton')?.addEventListener('click', (e)=>onSave(view, e), { capture: true });
      $(view, '#scanNowButton')?.addEventListener('click', (e)=>onScan(view, e), { capture: true });
      loadConfig(view);
      setStatus(view, 'Bereit');
      console.log('[CBF] Listener gebunden');
    });
  };
});
