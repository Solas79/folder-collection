(() => {
const pluginId = 'f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41';

function loadConfig(page) {
return ApiClient.getNamedConfiguration(pluginId, 'plugin').then(cfg => {
page.querySelector('#LibraryRoots').value = (cfg.LibraryRoots || []).join('\n');
page.querySelector('#Prefix').value = cfg.Prefix || '';
page.querySelector('#Suffix').value = cfg.Suffix || '';
page.querySelector('#Blacklist').value = (cfg.Blacklist || []).join('\n');
page.querySelector('#MinItemsPerFolder').value = cfg.MinItemsPerFolder || 2;
page.querySelector('#DailyScanEnabled').checked = !!cfg.DailyScanEnabled;
page.querySelector('#DailyScanTime').value = cfg.DailyScanTime || '03:30';
});
}

function saveConfig(page) {
return ApiClient.getNamedConfiguration(pluginId, 'plugin').then(cfg => {
cfg.LibraryRoots = page.querySelector('#LibraryRoots').value
.split(/\r?\n/).map(s => s.trim()).filter(Boolean);
cfg.Prefix = page.querySelector('#Prefix').value || '';
cfg.Suffix = page.querySelector('#Suffix').value || '';
cfg.Blacklist = page.querySelector('#Blacklist').value
.split(/\r?\n/).map(s => s.trim()).filter(Boolean);
cfg.MinItemsPerFolder = parseInt(page.querySelector('#MinItemsPerFolder').value || '2', 10);
cfg.DailyScanEnabled = page.querySelector('#DailyScanEnabled').checked;
cfg.DailyScanTime = page.querySelector('#DailyScanTime').value || '03:30';

return ApiClient.updateNamedConfiguration(pluginId, 'plugin', cfg).then(() => {
Dashboard.processPluginConfigurationUpdateResult();
});
});
}

document.addEventListener('pageshow', e => {
const page = e.target;
if (page.id !== 'CollectionsByFolderConfig') return;

loadConfig(page);

page.querySelector('#pluginConfigForm').addEventListener('submit', evt => {
evt.preventDefault();
saveConfig(page);
});

page.querySelector('#ScanNowBtn').addEventListener('click', () => {
ApiClient.ajax({
type: 'POST',
url: ApiClient.getUrl('Plugins/CollectionsByFolder/ScanNow')
}).then(() => {
Dashboard.alert('Scan gestartet');
}).catch(err => {
console.error(err);
Dashboard.alert('Scan konnte nicht gestartet werden');
});
});
});
})();
