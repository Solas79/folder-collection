(function(){
const pluginId = '4bb2a3d2-b8c6-4b3f-bf2c-d1a3e4e9b7a1';

function page(){ return document.querySelector('#folderCollectionsConfig'); }
function val(id){ return document.getElementById(id); }

document.addEventListener('viewshow', function(e){
if (!page() || !e.detail || !e.detail.state || e.detail.state.routeInfo?.path !== '/web/index.html#!/plugins/foldercollections/config.html') return;
load();
});

async function load(){
const cfg = await ApiClient.getPluginConfiguration(pluginId);
val('includeMovies').checked = cfg.IncludeMovies ?? true;
val('includeSeries').checked = cfg.IncludeSeries ?? true;
val('minItems').value = cfg.MinimumItemsPerFolder ?? 2;
val('useBasename').checked = cfg.UseBasenameForCollection ?? true;
val('namePrefix').value = cfg.NamePrefix ?? '';
val('nameSuffix').value = cfg.NameSuffix ?? '';
val('prefixes').value = (cfg.LibraryPathPrefixes||[]).join('\n');
val('ignores').value = (cfg.IgnorePatterns||[]).join('\n');
}

document.addEventListener('submit', async function(ev){
const form = ev.target;
if (!page() || form.id !== 'cfgForm') return;
ev.preventDefault();
const cfg = await ApiClient.getPluginConfiguration(pluginId);
cfg.IncludeMovies = val('includeMovies').checked;
cfg.IncludeSeries = val('includeSeries').checked;
cfg.MinimumItemsPerFolder = parseInt(val('minItems').value || '2', 10);
cfg.UseBasenameForCollection = val('useBasename').checked;
cfg.NamePrefix = val('namePrefix').value || '';
cfg.NameSuffix = val('nameSuffix').value || '';
cfg.LibraryPathPrefixes = val('prefixes').value.split(/\r?\n/).map(s=>s.trim()).filter(Boolean);
cfg.IgnorePatterns = val('ignores').value.split(/\r?\n/).map(s=>s.trim()).filter(Boolean);

const result = await ApiClient.updatePluginConfiguration(pluginId, cfg);
if (result?.Status === 'Invalid') {
Dashboard.alert({
message: result?.ErrorMessage || 'Speichern fehlgeschlagen',
title: 'Folder Collections'
});
} else {
Dashboard.processPluginConfigurationUpdateResult(result);
})();
