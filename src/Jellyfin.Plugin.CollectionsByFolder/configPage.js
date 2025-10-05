// Datei: configPage.js
define([], function () {
  const pluginId = 'f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41';

  function setStatus(msg) {
    const el = document.getElementById('cbf-status');
    if (el) el.textContent = msg;
    console.log('[CBF]', msg);
  }

  function loadConfig() {
    return ApiClient.getPluginConfiguration(pluginId).then(cfg => {
      document.getElementById('whitelist').value = (cfg.Whitelist || []).join('\n');
      document.getElementById('blacklist').value = (cfg.Blacklist || []).join('\n');
      document.getElementById('prefix').value    = cfg.Prefix || '';
      document.getElementById('suffix').value    = cfg.Suffix || '';
      document.getElementById('minfiles').value  = String(cfg.MinFiles || 0);
      setStatus('Konfiguration geladen');
    }).catch(err => setStatus('Fehler beim Laden: ' + (err?.message || err)));
  }

  function saveConfig(e) {
    e.preventDefault();
    setStatus('Speichern…');
    const cfg = {
      Whitelist: (document.getElementById('whitelist').value || '').split('\n').filter(Boolean),
      Blacklist: (document.getElementById('blacklist').value || '').split('\n').filter(Boolean),
      Prefix: document.getElementById('prefix').value || '',
      Suffix: document.getElementById('suffix').value || '',
      MinFiles: parseInt(document.getElementById('minfiles').value || '0', 10) || 0
    };
    ApiClient.updatePluginConfiguration(pluginId, cfg)
      .then(() => setStatus('Gespeichert ✔'))
      .catch(err => setStatus('Fehler: ' + (err?.message || err)));
  }

  function scanNow(e) {
    e.preventDefault();
    setStatus('Scan gestartet…');
    setTimeout(() => setStatus('Scan abgeschlossen ✔ (Demo)'), 800);
  }

  // Jellyfin ruft dieses Modul mit der View auf:
  return function (view) {
    view.addEventListener('viewshow', function () {
      document.getElementById('saveButton')?.addEventListener('click', saveConfig);
      document.getElementById('scanNowButton')?.addEventListener('click', scanNow);
      loadConfig();
      setStatus('Bereit');
      console.log('[CBF] Listener gebunden');
    });
  };
});
