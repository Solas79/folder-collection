define(['jellyfin-apiclient', 'emby-input', 'emby-button'], function (apiClient) {
    'use strict';

    const pluginId = 'f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41';

    function getConfig() {
        return apiClient.getPluginConfiguration(pluginId);
    }

    function updateConfig(config) {
        return apiClient.updatePluginConfiguration(pluginId, config);
    }

    async function saveConfig(page) {
        const config = await getConfig();

        config.Whitelist = page.querySelector('#whitelist').value.split('\n').map(x => x.trim()).filter(Boolean);
        config.Blacklist = page.querySelector('#blacklist').value.split('\n').map(x => x.trim()).filter(Boolean);
        config.Prefix = page.querySelector('#prefix').value.trim();
        config.Suffix = page.querySelector('#suffix').value.trim();

        await updateConfig(config);
        Dashboard.processPluginConfigurationUpdateResult();
    }

    async function runScan() {
        try {
            await apiClient.fetch({
                url: ApiClient.getUrl('CollectionsByFolder/RunNow'),
                method: 'POST'
            });
            alert('Scan gestartet!');
        } catch (err) {
            alert('Fehler beim Starten des Scans: ' + err.message);
        }
    }

    return {
        onShow: async function () {
            const page = document.querySelector('#collectionsByFolderPage');
            const config = await getConfig();

            page.querySelector('#whitelist').value = (config.Whitelist || []).join('\n');
            page.querySelector('#blacklist').value = (config.Blacklist || []).join('\n');
            page.querySelector('#prefix').value = config.Prefix || '';
            page.querySelector('#suffix').value = config.Suffix || '';

            page.querySelector('#saveButton').addEventListener('click', function () {
                saveConfig(page);
            });

            page.querySelector('#runButton').addEventListener('click', function () {
                runScan();
            });
        }
    };
});
