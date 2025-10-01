// src/FolderCollections/Configuration/config.js
(() => {
  const pluginId = '9f4f2c47-b3c5-4b13-9b1f-1c9a5c3b8d6a'; // exakt wie in Plugin.cs

  function toLines(arr) {
    return Array.isArray(arr) ? arr.join('\n') : (arr || '');
  }
  function fromLines(text) {
    return (text || '')
      .split('\n')
      .map(s => s.trim())
      .filter(Boolean);
  }

  async function load() {
    const cfg = await ApiClient.getPluginConfiguration(pluginId);

    document.querySelector('#includeMovies').checked = !!cfg.IncludeMovies;
    document.querySelector('#includeSeries').checked = !!cfg.IncludeSeries;
    document.querySelector('#minItems').value = cfg.MinItems ?? 2;
    document.querySelector('#prefix').value = cfg.Prefix ?? '';
    document.querySelector('#suffix').value = cfg.Suffix ?? '';
    document.querySelector('#scanHour').value = cfg.ScanHour ?? 4;
    document.querySelector('#pathPrefixes').value = toLines(cfg.PathPrefixes);
    document.querySelector('#ignorePatterns').value = toLines(cfg.IgnorePatterns);
  }

  async function save() {
    const cfg = await ApiClient.getPluginConfiguration(pluginId);

    cfg.IncludeMovies = document.querySelector('#includeMovies').checked;
    cfg.IncludeSeries = document.querySelector('#includeSeries').checked;
    cfg.MinItems = parseInt(document.querySelector('#minItems').value, 10) || 0;
    cfg.Prefix = document.querySelector('#prefix').value.trim();
    cfg.Suffix = document.querySelector('#suffix').value.trim();
    cfg.ScanHour = Math.min(23, Math.max(0, parseInt(document.querySelector('#scanHour').value, 10) || 4));
    cfg.PathPrefixes = fromLines(document.querySelector('#pathPrefixes').value);
    cfg.IgnorePatterns = fromLines(document.querySelector('#ignorePatterns').value);

    const result = await ApiClient.updatePluginConfiguration(pluginId, cfg);
    Dashboard.processPluginConfigurationUpdateResult(result);
  }

  document.addEventListener('pageshow', (e) => {
    if (e.target.id === 'folderCollectionsConfigPage') load().catch(console.error);
  });

  document.addEventListener('click', (e) => {
    if (e.target.classList.contains('button-submit')) {
      e.preventDefault();
      save().catch(console.error);
    } else if (e.target.classList.contains('button-cancel')) {
      e.preventDefault();
      history.back();
    }
  });
})();
