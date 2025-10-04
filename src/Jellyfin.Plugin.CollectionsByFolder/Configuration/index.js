<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <title>Collections by Folder</title>
</head>
<body>
  <div id="collectionsByFolderPage"
       data-role="page"
       class="page type-interior pluginConfigurationPage"
       data-require="emby-button,emby-input,emby-select"
       data-pluginid="f58f3a40-6a8a-48e8-9b3a-9d7f0b6a3a41">

    <div data-role="content" class="content-primary" role="main">
      <h2>Sammlungen nach Ordnern</h2>
      <p>Erstellt Collections anhand des letzten Ordnernamens.</p>

      <div class="inputContainer">
        <label class="inputLabel" for="prefix">Präfix</label>
        <input is="emby-input" id="prefix" type="text" />
      </div>

      <div class="inputContainer">
        <label class="inputLabel" for="suffix">Suffix</label>
        <input is="emby-input" id="suffix" type="text" />
      </div>

      <div class="inputContainer">
        <label class="inputLabel" for="minItems">Mindestanzahl</label>
        <input is="emby-input" id="minItems" type="number" min="1" value="1" />
      </div>

      <div class="inputContainer">
        <label class="checkboxLabel">
          <input type="checkbox" id="enableDailyScan" />
          <span>Täglichen Scan aktivieren</span>
        </label>
      </div>

      <div class="inputContainer">
        <label class="inputLabel" for="scanTime">Uhrzeit (HH:MM)</label>
        <input is="emby-input" id="scanTime" type="time" value="03:00" />
      </div>

      <div class="inputContainer">
        <label class="inputLabel" for="blacklist">Blacklist (kommasepariert)</label>
        <input is="emby-input" id="blacklist" type="text" />
      </div>

      <div class="fieldSet" style="display:flex;gap:.5rem;flex-wrap:wrap;margin-top:.5rem;">
        <button is="emby-button" type="button" class="raised" id="saveButton"><span>Speichern</span></button>
        <button is="emby-button" type="button" class="raised" id="scanNowButton"><span>Jetzt scannen</span></button>
      </div>

      <div id="cbf-status" style="margin-top:10px;"></div>
    </div> <!-- /content-primary -->
  </div> <!-- /page -->

  <!-- MUSS zum PluginPageInfo-Name der JS-Ressource passen -->
  <script src="collectionsbyfolderjs"></script>
</body>
</html>
