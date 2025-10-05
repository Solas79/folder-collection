/* AMD-Modul, wie von Jellyfin-Pluginseiten erwartet */
define([], function () {
  'use strict';

  const pluginId = 'f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41';

  function setStatus(msg) {
    const el = document.getElementById('cbf-status');
    if (el) el.textContent = msg;
    console.log('[CBF]', msg);
  }

  function loadConfig() {
    if (!window.ApiClient?.getPluginConfiguration) {
      setStatus('ApiClient nicht verfügbar');
      return Promise.resolve();
    }
    return ApiClient.getPluginConfiguration(pluginId).then(cfg => {
      document.getElementById('whitelist').value = (cfg.Whitelist || []).join('\n');
      document.getElementById('blacklist').value = (cfg.Blacklist || []).join('\n');
      document.getElementById('prefix').value    = cfg.Prefix || '';
      document.getElementById('suffix').value    = cfg.Suffix || '';
      document.getElementById('minfiles').value  = String(cfg.MinFiles || 0);
      setStatus('Konfiguration geladen');
    }).catch(err => setStatus('Fehler beim Laden: ' + (err?.message || err)));
  }

  function onSave(e) {
    e.preventDefault();
    setStatus('Speichern…');
    const cfg = {
      Whitelist: (document.getElementById('whitelist').value || '').split('\n').filter(Boolean),
      Blacklist: (document.getElementById('blacklist').value || '').split('\n').filter(Boolean),
      Prefix: document.getElementById('prefix').value || '',
      Suffix: document.getElementById('suffix').value || '',
      MinFiles: parseInt(document.getElementById('minfiles').value || '0', 10) || 0
    };
    if (!window.ApiClient?.updatePluginConfiguration) {
      setStatus('Kein ApiClient verfügbar');
      return;
    }
    ApiClient.updatePluginConfiguration(pluginId, cfg)
      .then(() => setStatus('Gespeichert ✔'))
      .catch(err => setStatus('Fehler: ' + (err?.message || err)));
  }

  function onScan(e) {
    e.preventDefault();
    setStatus('Scan gestartet…');
    // TODO: Hier später echten Server-Call einhängen
    setTimeout(() => setStatus('Scan abgeschlossen ✔ (Demo)'), 800);
  }

  // Jellyfin ruft das zurückgegebene Init mit der View auf
  return function (view) {
    view.addEventListener('viewshow', function () {
      document.getElementById('saveButton')?.addEventListener('click', onSave);
      document.getElementById('scanNowButton')?.addEventListener('click', onScan);
      loadConfig();
      setStatus('Bereit');
      console.log('[CBF] Listener gebunden');
    });
  };
});
