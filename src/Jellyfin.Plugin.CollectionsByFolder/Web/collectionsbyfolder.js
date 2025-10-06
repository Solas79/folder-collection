// UMD + Auto-Init: läuft mit AMD (define) UND direkt per <script>
(function (root, factory) {
  if (typeof define === 'function' && define.amd) {
    // AMD: Jellyfin kann das Modul weiterhin verwenden
    define([], function () { return factory(root, /*autoInit*/ false); });
  } else {
    // Direkt per <script>: sofort ausführen
    factory(root, /*autoInit*/ true);
  }
})(typeof window !== 'undefined' ? window : this, function (win, autoInit) {
  'use strict';

  const pluginId = 'f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41';

  function $(root, sel) { return root.querySelector(sel); }
  function setStatus(view, msg) {
    const el = $(view, '#cbf-status'); if (el) el.textContent = msg;
    try { console.log('[CBF]', msg); } catch {}
  }
  function linesToList(t) {
    return (t || '').replace(/\r\n?/g, '\n').split('\n').map(s => s.trim()).filter(Boolean);
  }

  function loadConfig(view) {
    if (!win.ApiClient || typeof win.ApiClient.getPluginConfiguration !== 'function') {
      setStatus(view, 'ApiClient nicht verfügbar');
      return Promise.resolve();
    }
    return win.ApiClient.getPluginConfiguration(pluginId).then(cfg => {
      $(view, '#whitelist').value = (cfg.Whitelist || []).join('\n');
      $(view, '#blacklist').value = (cfg.Blacklist || []).join('\n');
      $(view, '#prefix').value    = cfg.Prefix || '';
      $(view, '#suffix').value    = cfg.Suffix || '';
      $(view, '#minfiles').value  = String(cfg.MinFiles ?? 0);
      setStatus(view, 'Konfiguration geladen');
    }).catch(e => setStatus(view, 'Fehler beim Laden: ' + (e?.message || e)));
  }

  function onSave(view, e) {
    e?.preventDefault?.(); e?.stopPropagation?.(); e?.stopImmediatePropagation?.();
    setStatus(view, 'Speichern…');

    const cfg = {
      Whitelist:   linesToList($(view, '#whitelist').value),
      Blacklist:   linesToList($(view, '#blacklist').value),
      Prefix:      $(view, '#prefix').value || '',
      Suffix:      $(view, '#suffix').value || '',
      MinFiles:    parseInt($(view, '#minfiles').value || '0', 10) || 0,
      // Fallback-Feld:
      FolderPaths: linesToList($(view, '#whitelist').value)
    };

    return win.ApiClient.updatePluginConfiguration(pluginId, cfg)
      .then(() => setStatus(view, 'Gespeichert ✔'))
      .catch(e => setStatus(view, 'Fehler: ' + (e?.message || e)));
  }

  function onScan(view, e) {
    e?.preventDefault?.(); e?.stopPropagation?.(); e?.stopImmediatePropagation?.();
    setStatus(view, 'Scan gestartet…');
    setTimeout(() => setStatus(view, 'Scan abgeschlossen ✔ (Demo)'), 800);
  }

  function wire(view) {
    if (!view || view.__cbf_wired) return;
    view.__cbf_wired = true;

    // Sofort binden (nicht nur auf 'viewshow' verlassen)
    $(view, '#saveButton')?.addEventListener('click', e => onSave(view, e), { capture: true });
    $(view, '#scanNowButton')?.addEventListener('click', e => onScan(view, e), { capture: true });

    // Zusätzlich kompatibel mit Jellyfin-Event
    view.addEventListener('viewshow', function () {
      $(view, '#saveButton')?.addEventListener('click', e => onSave(view, e), { capture: true });
      $(view, '#scanNowButton')?.addEventListener('click', e => onScan(view, e), { capture: true });
      setStatus(view, 'Bereit');
    }, { once: true });

    loadConfig(view);
    setStatus(view, 'Bereit');
    try { console.log('[CBF] Listener gebunden'); } catch {}
  }

  function findView() {
    return document.getElementById('cbfPage')
        || document.querySelector('.pluginConfigurationPage[data-pluginid="' + pluginId + '"]')
        || document.querySelector('.pluginConfigurationPage');
  }

  // Für AMD (Jellyfin) das Init exportieren:
  win.__CBF_INIT__ = wire;

  if (autoInit) {
    // Direkt per <script>: automatisch initialisieren
    const run = () => { const v = findView(); if (v) wire(v); };
    if (document.readyState !== 'loading') run();
    else document.addEventListener('DOMContentLoaded', run, { once: true });
    document.addEventListener('viewshow', run, { capture: true });
    win.addEventListener('hashchange', () => setTimeout(run, 0));
  }

  return wire;
});
