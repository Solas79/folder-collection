// configPage.js
(function () {
  function setStatus(msg) {
    const el = document.getElementById('cbf-status');
    if (el) el.textContent = msg;
    console.log('[CBF]', msg);
  }

  function onSaveClick(e) {
    e.preventDefault();
    setStatus('Speichern…');

    const config = {
      Whitelist: (document.getElementById('whitelist')?.value || '').split('\n'),
      Blacklist: (document.getElementById('blacklist')?.value || '').split('\n'),
      Prefix: document.getElementById('prefix')?.value || '',
      Suffix: document.getElementById('suffix')?.value || '',
      MinFiles: parseInt(document.getElementById('minfiles')?.value || '0', 10) || 0
    };

    // Optional: gegen Jellyfin speichern, wenn ApiClient da ist
    const pluginId = 'cda3a99a-7db0-4a69-a9c4-2238e1d5b9b4';
    if (typeof window.ApiClient?.updatePluginConfiguration === 'function') {
      window.ApiClient.updatePluginConfiguration(pluginId, config)
        .then(() => setStatus('Gespeichert ✔'))
        .catch(err => setStatus('Fehler: ' + (err?.message || err)));
    } else {
      setStatus('Klick erkannt ✔ (Demo: kein ApiClient gefunden)');
    }
  }

  function onScanClick(e) {
    e.preventDefault();
    setStatus('Scan gestartet…');
    // Hier später deinen echten Scan-Endpoint / API-Aufruf einbauen
    setTimeout(() => setStatus('Scan abgeschlossen ✔ (Demo)'), 800);
  }

  function init() {
    const saveBtn = document.getElementById('saveButton');
    const scanBtn = document.getElementById('scanNowButton');

    if (!saveBtn || !scanBtn) {
      console.warn('[CBF] Buttons nicht gefunden – ist die richtige HTML-Seite geladen?');
      return;
    }

    saveBtn.addEventListener('click', onSaveClick);
    scanBtn.addEventListener('click', onScanClick);

    setStatus('Seite bereit – Listener aktiv');
    console.log('[CBF] Listener gebunden');
  }

  // Warten, bis DOM da ist (Jellyfin lädt als Single-Page-App)
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
