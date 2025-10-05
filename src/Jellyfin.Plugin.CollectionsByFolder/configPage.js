/* Jellyfin Config-Page JS (AMD)
   Route (Beispiel): /web/collectionsbyfolderjs
   Plugin.cs:
     new PluginPageInfo { Name = "collectionsbyfolderjs", EmbeddedResourcePath = "Jellyfin.Plugin.CollectionsByFolder.configPage.js" }
   HTML (configPage.html):
     <script src="collectionsbyfolderjs"></script>
*/
define([], function () {
  'use strict';

  // ← deine Plugin-GUID (muss mit Plugin.Id übereinstimmen)
  const pluginId = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee';

  // Helpers (auf die aktuelle View scopen)
  function q(view, sel)       { return view.querySelector(sel); }
  function byId(view, id)     { return view.querySelector('#' + id); }
  function val(el)            { return (el && el.value != null) ? el.value : ''; }
  function linesToList(text)  { return (text || '').split('\n').map(s => s.trim()).filter(Boolean); }

  function setStatus(view, msg) {
    const el = byId(view, 'cbf-status');
    if (el) el.textContent = msg;
    console.log('[CBF]', msg);
  }

  function loadConfig(view) {
    if (!window.ApiClient?.getPluginConfiguration) {
      setStatus(view, 'ApiClient nicht verfügbar');
      return Promise.resolve();
    }
    return ApiClient.getPluginConfiguration(pluginId).then(cfg => {
      byId(view, 'whitelist').value = (cfg?.Whitelist || []).join('\n');
      byId(view, 'blacklist').value = (cfg?.Blacklist || []).join('\n');
      byId(view, 'prefix').value    = cfg?.Prefix || '';
      byId(view, 'suffix').value    = cfg?.Suffix || '';
      byId(view, 'minfiles').value  = String(cfg?.MinFiles ?? 0);
      setStatus(view, 'Konfiguration geladen');
    }).catch(err => setStatus(view, 'Fehler beim Laden: ' + (err?.message || err)));
  }

  function onSave(view, e) {
    e?.preventDefault();
    setStatus(view, 'Speichern…');

    const cfg = {
      Whitelist: linesToList(val(byId(view, 'whitelist'))),
      Blacklist: linesToList(val(byId(view, 'blacklist'))),
      Prefix:    val(byId(view, 'prefix')),
      Suffix:    val(byId(view, 'suffix')),
      MinFiles:  parseInt(val(byId(view, 'minfiles')) || '0', 10) || 0
    };

    if (!window.ApiClient?.updatePluginConfiguration) {
      setStatus(view, 'Kein ApiClient verfügbar');
      return;
    }
    ApiClient.updatePluginConfiguration(pluginId, cfg)
      .then(() => setStatus(view, 'Gespeichert ✔'))
      .catch(err => setStatus(view, 'Fehler: ' + (err?.message || err)));
  }

  function onScan(view, e) {
    e?.preventDefault();
    setStatus(view, 'Scan gestartet…');
    // TODO: Hier später echten Server-Call einbauen (Controller/Task)
    setTimeout(() => setStatus(view, 'Scan abgeschlossen ✔ (Demo)'), 800);
  }

  // Jellyfin ruft das zurückgegebene Init mit der View auf
  return function (view) {
    // nur einmal pro Anzeige initialisieren
    view.addEventListener('viewshow', function handleShow() {
      view.removeEventListener('viewshow', handleShow);

      byId(view, 'saveButton')?.addEventListener('click', (e) => onSave(view, e));
      byId(view, 'scanNowButton')?.addEventListener('click', (e) => onScan(view, e));

      loadConfig(view).finally(() => setStatus(view, 'Bereit'));
      console.log('[CBF] Listener gebunden');
    });
  };
});
