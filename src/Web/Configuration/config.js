(() => {
  'use strict';

  // <- Deinen Plugin-GUID hier eintragen:
  const pluginId = '00000000-0000-0000-0000-000000000000';

  const SELECTORS = {
    page:   '#FolderCollectionsConfigurationPage',
    form:   '#fcForm',
    root:   '#fcRoot',
    pattern:'#fcPattern',
    enable: '#fcEnableScan',
    save:   '#fcSave'
  };

  function log(...args) {
    console.debug('[FolderCollections][config]', ...args);
  }

  function showLoading() {
    try { Dashboard.showLoadingMsg(); } catch {}
  }
  function hideLoading() {
    try { Dashboard.hideLoadingMsg(); } catch {}
  }

  async function loadConfig() {
    showLoading();
    try {
      const cfg = await ApiClient.getPluginConfiguration(pluginId);
      log('geladen:', cfg);

      document.querySelector(SELECTORS.root).value    = cfg.RootPath || '';
      document.querySelector(SELECTORS.pattern).value = cfg.Pattern  || '';
      document.querySelector(SELECTORS.enable).checked = !!cfg.EnableScan;
    } catch (err) {
      console.error('[FolderCollections] Laden fehlgeschlagen:', err);
      Dashboard.alert('Konfiguration konnte nicht geladen werden. Siehe Browser-Konsole.');
    } finally {
      hideLoading();
    }
  }

  async function saveConfig(ev) {
    if (ev) ev.preventDefault();
    showLoading();
    try {
      // Bestehende Konfiguration holen, dann überschreiben (falls Plugin später mehr Felder bekommt)
      const cfg = await ApiClient.getPluginConfiguration(pluginId);

      cfg.RootPath   = document.querySelector(SELECTORS.root).value.trim();
      cfg.Pattern    = document.querySelector(SELECTORS.pattern).value.trim();
      cfg.EnableScan = !!document.querySelector(SELECTORS.enable).checked;

      const result = await ApiClient.updatePluginConfiguration(pluginId, cfg);
      Dashboard.processPluginConfigurationUpdateResult(result);
    } catch (err) {
      console.error('[FolderCollections] Speichern fehlgeschlagen:', err);
      Dashboard.alert('Konfiguration konnte nicht gespeichert werden. Siehe Browser-Konsole.');
    } finally {
      hideLoading();
    }
  }

  // In Jellyfin feuert bei Plugin-Seiten zuverlässig "pageshow" für das konkrete Page-Element
  function wirePage() {
    const page = document.querySelector(SELECTORS.page);
    if (!page) return;

    // Beim Anzeigen der Seite Config laden
    page.addEventListener('pageshow', loadConfig);

    // Speichern-Button/Form
    const form = document.querySelector(SELECTORS.form);
    if (form) {
      form.addEventListener('submit', saveConfig);
    } else {
      // Fallback: einzelner Button
      const btn = document.querySelector(SELECTORS.save);
      if (btn) btn.addEventListener('click', saveConfig);
    }
  }

  // Sicherstellen, dass das DOM steht
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', wirePage);
  } else {
    wirePage();
  }
})();
