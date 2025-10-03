/* global ApiClient, Dashboard */
define([], function () {
  // MUSS identisch zu Plugin.PluginGuid sein:
  const pluginId = '9f4f2c47-b3c5-4b13-9b1f-1c2d3e4f5a60';

  function loadConfig(view) {
    return ApiClient.getPluginConfiguration(pluginId).then(cfg => {
      cfg = cfg || {};
      view.querySelector('#fc-root').value = cfg.RootPath || '';
      view.querySelector('#fc-recursive').checked = !!cfg.Recursive;
      view.querySelector('#fc-prefix').value = cfg.CollectionNamePrefix || 'FC:';
    }).catch(err => {
      console.error('[FolderCollections] Load config failed:', err);
      Dashboard?.alert?.('Konfiguration konnte nicht geladen werden.');
    });
  }

  function saveConfig(view) {
    return ApiClient.getPluginConfiguration(pluginId).then(cfg => {
      cfg = cfg || {};
      cfg.RootPath = (view.querySelector('#fc-root').value || '').trim();
      cfg.Recursive = !!view.querySelector('#fc-recursive').checked;
      cfg.CollectionNamePrefix = (view.querySelector('#fc-prefix').value || 'FC:').trim();
      return ApiClient.updatePluginConfiguration(pluginId, cfg);
    }).then(result => {
      Dashboard?.processPluginConfigurationUpdateResult?.(result);
    }).catch(err => {
      console.error('[FolderCollections] Save failed:', err);
      Dashboard?.alert?.('Speichern fehlgeschlagen.');
    });
  }

  function scanNow() {
    return ApiClient.ajax({
      type: 'POST',
      url: ApiClient.getUrl('FolderCollections/Configuration/Scan')
    }).then(() => {
      Dashboard?.alert?.('Scan gestartet.');
    }).catch(err => {
      console.error('[FolderCollections] Scan failed:', err);
      Dashboard?.alert?.('Scan konnte nicht gestartet werden.');
    });
  }

  // Entry
  return function (view) {
    console.info('[FolderCollections] controller init');
    view.addEventListener('viewshow', function () { loadConfig(view); });
    view.querySelector('#fc-save')?.addEventListener('click', () => { saveConfig(view); });
    view.querySelector('#fc-scan')?.addEventListener('click', () => { scanNow(); });
  };
});
