define([], function () {
    'use strict';

    const pluginId = '4bb2a3d2-b8c6-4b3f-bf2c-d1a3e4e9b7a1'; // muss exakt der Id in Plugin.cs entsprechen

    function byId(view, id) {
        return view.querySelector('#' + id);
    }

    return function (view) {

        async function load() {
            const cfg = await ApiClient.getPluginConfiguration(pluginId);

            byId(view, 'includeMovies').checked = !!cfg.IncludeMovies;
            byId(view, 'includeSeries').checked = !!cfg.IncludeSeries;
            byId(view, 'minItems').value = cfg.MinimumItemsPerFolder ?? 2;
            byId(view, 'useBasename').checked = !!cfg.UseBasenameForCollection;
            byId(view, 'namePrefix').value = cfg.NamePrefix || '';
            byId(view, 'nameSuffix').value = cfg.NameSuffix || '';
            byId(view, 'prefixes').value = (cfg.LibraryPathPrefixes || []).join('\n');
            byId(view, 'ignores').value = (cfg.IgnorePatterns || []).join('\n');
        }

        async function save(ev) {
            ev.preventDefault();

            const cfg = await ApiClient.getPluginConfiguration(pluginId);
            cfg.IncludeMovies = byId(view, 'includeMovies').checked;
            cfg.IncludeSeries = byId(view, 'includeSeries').checked;
            cfg.MinimumItemsPerFolder = parseInt(byId(view, 'minItems').value || '2', 10);
            cfg.UseBasenameForCollection = byId(view, 'useBasename').checked;
            cfg.NamePrefix = byId(view, 'namePrefix').value || '';
            cfg.NameSuffix = byId(view, 'nameSuffix').value || '';
            cfg.LibraryPathPrefixes = byId(view, 'prefixes').value.split(/\r?\n/).map(s => s.trim()).filter(Boolean);
            cfg.IgnorePatterns = byId(view, 'ignores').value.split(/\r?\n/).map(s => s.trim()).filter(Boolean);

            const result = await ApiClient.updatePluginConfiguration(pluginId, cfg);
            if (result?.Status === 'Invalid') {
                Dashboard.alert({
                    title: 'Folder Collections',
                    message: result?.ErrorMessage || 'Speichern fehlgeschlagen'
                });
            } else {
                Dashboard.processPluginConfigurationUpdateResult(result);
                Dashboard.alert({ title: 'Folder Collections', message: 'Gespeichert' });
            }
        }

        view.addEventListener('viewshow', load);
        view.querySelector('#cfgForm').addEventListener('submit', save);
    };
});
