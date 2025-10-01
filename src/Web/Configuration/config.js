define([], function () {
  'use strict';

  const pluginId = '4bb2a3d2-b8c6-4b3f-bf2c-d1a3e4e9b7a1'; // = Id aus Plugin.cs

  function q(view, id) { return view.querySelector('#' + id); }

  // ScheduledTasks-API: getTasks / startTask / updateTask (offiziell vorhanden) 
  // startTask(taskId), updateTask(taskId, TaskTriggerInfo[]) – vgl. Jellyfin TS SDK. 
  // (Quelle: Jellyfin TypeScript SDK – ScheduledTasksApi) 
  async function getOurTaskId() {
    const tasks = await ApiClient.getJSON(ApiClient.getUrl('ScheduledTasks')); // Liste aller Tasks
    const t = tasks.find(x => x.Key === 'FolderCollectionsTask' || x.Name?.includes('Folder Collections'));
    return t ? t.Id : null;
  }

  async function startOurTask() {
    const id = await getOurTaskId();
    if (!id) throw new Error('Task nicht gefunden');
    await ApiClient.fetch(ApiClient.getUrl('ScheduledTasks/' + id + '/Start'), { method: 'POST' }); // startTask
  }

  async function updateTrigger(hour, minute) {
    const id = await getOurTaskId();
    if (!id) return;
    const triggers = [{
      Type: 'Daily',
      TimeOfDayTicks: (hour * 3600 + minute * 60) * 10_000_000 // Sekunden → Ticks
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
      cfg.LibraryPathPrefixes = q(view, 'prefixes').value.split(/\r?\n/).map(s=>s.trim()).filter(Boolean);
      cfg.IgnorePatterns = q(view, 'ignores').value.split(/\r?\n/).map(s=>s.trim()).filter(Boolean);

      cfg.ScanHour = Math.min(23, Math.max(0, parseInt(q(view, 'scanHour').value||'4',10)));
      cfg.ScanMinute = Math.min(59, Math.max(0, parseInt(q(view, 'scanMinute').value||'0',10)));

      const result = await ApiClient.updatePluginConfiguration(pluginId, cfg);
      Dashboard.processPluginConfigurationUpdateResult(result);
      await updateTrigger(cfg.ScanHour, cfg.ScanMinute); // neuen Trigger setzen
      Dashboard.alert({ title: 'Folder Collections', message: 'Gespeichert' });
    }

    async function runNow() {
      try {
        await startOurTask();
        Dashboard.alert({ title: 'Folder Collections', message: 'Task gestartet' });
      } catch (e) {
        Dashboard.alert({ title: 'Folder Collections', message: 'Task konnte nicht gestartet werden: ' + e });
      }
    }

    view.addEventListener('viewshow', load);
    q(view, 'cfgForm').addEventListener('submit', save);
    q(view, 'runNow').addEventListener('click', runNow);
  };
});
