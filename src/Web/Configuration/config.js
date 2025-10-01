define([], function () {
  'use strict';

  // MUSS exakt der Id in Plugin.cs entsprechen!
  const pluginId = '4bb2a3d2-b8c6-4b3f-bf2c-d1a3e4e9b7a1';

  function q(view, id) { return view.querySelector('#' + id); }

  async function getOurTaskId() {
    const tasks = await ApiClient.getJSON(ApiClient.getUrl('ScheduledTasks'));
    const t = tasks.find(x => x.Key === 'FolderCollectionsTask' || (x.Name || '').toLowerCase().includes('folder collections'));
    return t ? t.Id : null;
  }

  async function startOurTask() {
    const id = await getOurTaskId();
    if (!id) throw new Error('Scheduled Task nicht gefunden');
    await ApiClient.fetch(ApiClient.getUrl('ScheduledTasks/' + id + '/Start'), { method: 'POST' });
  }

  async function updateTrigger(hour, minute) {
    const id = await getOurTaskId();
    if (!id) return;
    const triggers = [{
      Type: 'Daily',
      TimeOfDayTicks: (hour * 3600 + minute * 60) * 10000000
    }];
    await ApiClient.ajax({
      url: ApiClient.getUrl('ScheduledTasks/' + id),
      type: 'POST',
      data: JSON.stringify(triggers),
      contentType: 'application/json'
    });
  }

  return function (view) {
    async function load() {
      const cfg = await ApiClient.getPluginConfiguration(pluginId);

      q(view, 'includeMovies').checked = !!cfg.IncludeMovies;
      q(view, 'includeSeries').checked = !!cfg.IncludeSeries;
      q(view, 'minItems').value = cfg.MinimumItemsPerFolder ?? 2;
      q(view, 'useBasename').checked = !!cfg.UseBasenameForCollection;
      q(view, 'namePrefix').value = cfg.NamePrefix || '';
      q(view, 'nameSuffix').value = cfg.NameSuffix || '';
      q(view, 'prefixes').value = (cfg.LibraryPathPrefixes || []).join('\n');
      q(view, 'ignores').value = (cfg.IgnorePatterns || []).join('\n');

      q(view, 'scanHour').value = cfg.ScanHour ?? 4;
      q(view, 'scanMinute').value = cfg.ScanMinute ?? 0;
    }

    async function save(ev) {
      ev.preventDefault();
      const cfg = await ApiClient.getPluginConfiguration(pluginId);
      cfg.IncludeMovies = q(view, 'includeMovies').checked;
      cfg.IncludeSeries = q(view, 'includeSeries').checked;
      cfg.MinimumItemsPerFolder = parseInt(q(view, 'minItems').value || '2', 10);
      cfg.UseBasenameForCollection = q(view, 'useBasename').checked;
      cfg.NamePrefix = q(view, 'namePrefix').value || '';
      cfg.NameSuffix = q(view, 'nameSuffix').value || '';
      cfg.LibraryPathPrefixes = q(view, 'prefixes').value.split(/\r?\n/).map(s => s.trim()).filter(Boolean);
      cfg.IgnorePatterns = q(view, 'ignores').value.split(/\r?\n/).map(s => s.trim()).filter(Boolean);

      const h = Math.min(23, Math.max(0, parseInt(q(view, 'scanHour').value   || '4', 10)));
      const m = Math.min(59, Math.max(0, parseInt(q(view, 'scanMinute').value || '0', 10)));
      cfg.ScanHour = h; cfg.ScanMinute = m;

      const result = await ApiClient.updatePluginConfiguration(pluginId, cfg);
      Dashboard.processPluginConfigurationUpdateResult(result);
      await updateTrigger(h, m);
      Dashboard.alert({ title: 'Folder Collections', message: 'Gespeichert' });
    }

    async function runNow() {
      try { await startOurTask(); Dashboard.alert({ title: 'Folder Collections', message: 'Scan gestartet.' }); }
      catch { Dashboard.alert({ title: 'Folder Collections', message: 'Scan konnte nicht gestartet werden.' }); }
    }

    view.addEventListener('viewshow', load);
    q(view, 'cfgForm').addEventListener('submit', save);
    q(view, 'runNow').addEventListener('click', runNow);
  };
});
