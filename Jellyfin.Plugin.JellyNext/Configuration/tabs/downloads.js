// Downloads tab initialization and management
function initDownloadsTab() {
    // Initialize download integration toggle
    var config = JellyNextConfig;
    var downloadIntegration = config.DownloadIntegration !== undefined ? config.DownloadIntegration : 0;

    if (downloadIntegration === 0 || downloadIntegration === "Native") {
        downloadIntegration = 0;
    } else if (downloadIntegration === "Jellyseerr") {
        downloadIntegration = 1;
    } else if (downloadIntegration === "Webhook") {
        downloadIntegration = 2;
    }

    toggleDownloadIntegration(downloadIntegration);

    // Set up event listeners
    setupDownloadEventListeners();
}

// Toggle download integration sections
function toggleDownloadIntegration(integrationType) {
    var nativeSection = document.getElementById('NativeIntegrationSection');
    var jellyseerrSection = document.getElementById('JellyseerrSection');
    var webhookSection = document.getElementById('WebhookSection');
    var nativeOption = document.getElementById('NativeIntegrationOption');
    var jellyseerrOption = document.getElementById('JellyseerrIntegrationOption');
    var webhookOption = document.getElementById('WebhookIntegrationOption');

    // Reset all styles
    nativeOption.style.borderColor = '#444';
    nativeOption.style.backgroundColor = 'transparent';
    jellyseerrOption.style.borderColor = '#444';
    jellyseerrOption.style.backgroundColor = 'transparent';
    webhookOption.style.borderColor = '#444';
    webhookOption.style.backgroundColor = 'transparent';

    if (integrationType === 0 || integrationType === '0') {
        // Native integration
        nativeSection.style.display = 'block';
        jellyseerrSection.style.display = 'none';
        webhookSection.style.display = 'none';
        nativeOption.style.borderColor = '#00a4dc';
        nativeOption.style.backgroundColor = 'rgba(0, 164, 220, 0.1)';
    } else if (integrationType === 1 || integrationType === '1') {
        // Jellyseerr integration
        nativeSection.style.display = 'none';
        jellyseerrSection.style.display = 'block';
        webhookSection.style.display = 'none';
        jellyseerrOption.style.borderColor = '#00a4dc';
        jellyseerrOption.style.backgroundColor = 'rgba(0, 164, 220, 0.1)';
    } else {
        // Webhook integration
        nativeSection.style.display = 'none';
        jellyseerrSection.style.display = 'none';
        webhookSection.style.display = 'block';
        webhookOption.style.borderColor = '#00a4dc';
        webhookOption.style.backgroundColor = 'rgba(0, 164, 220, 0.1)';
    }
}

// Toggle Jellyseerr Radarr manual configuration visibility
function toggleJellyseerrRadarrManualConfig() {
    var useDefaults = document.getElementById('UseJellyseerrRadarrDefaults').checked;
    var manualConfig = document.getElementById('JellyseerrRadarrManualConfig');
    manualConfig.style.display = useDefaults ? 'none' : 'block';
}

// Toggle Jellyseerr Sonarr manual configuration visibility
function toggleJellyseerrSonarrManualConfig() {
    var useDefaults = document.getElementById('UseJellyseerrSonarrDefaults').checked;
    var manualConfig = document.getElementById('JellyseerrSonarrManualConfig');
    manualConfig.style.display = useDefaults ? 'none' : 'block';
}

// Load Jellyseerr servers and profiles
function loadJellyseerrServers(jellyseerrUrl, jellyseerrApiKey) {
    var encodedUrl = encodeURIComponent(jellyseerrUrl);
    var encodedApiKey = encodeURIComponent(jellyseerrApiKey);

    // Load Radarr servers
    ApiClient.fetch({
        type: 'GET',
        url: ApiClient.getUrl('JellyNext/Jellyseerr/Radarr/Servers?jellyseerrUrl=' + encodedUrl + '&apiKey=' + encodedApiKey)
    }).then(function (response) {
        return response.json();
    }).then(function (servers) {
        JellyNextConfig.jellyseerrRadarrServers = servers || [];
        populateJellyseerrRadarrServers();

        // Load Sonarr servers
        return ApiClient.fetch({
            type: 'GET',
            url: ApiClient.getUrl('JellyNext/Jellyseerr/Sonarr/Servers?jellyseerrUrl=' + encodedUrl + '&apiKey=' + encodedApiKey)
        });
    }).then(function (response) {
        return response.json();
    }).then(function (servers) {
        JellyNextConfig.jellyseerrSonarrServers = servers || [];
        populateJellyseerrSonarrServers();

        Dashboard.hideLoadingMsg();

        // Show sections
        document.getElementById('JellyseerrRadarrSection').style.display = 'block';
        document.getElementById('JellyseerrSonarrSection').style.display = 'block';
    }).catch(function (error) {
        Dashboard.hideLoadingMsg();
        console.error('Error loading Jellyseerr servers:', error);
        Dashboard.alert('Failed to load server configurations: ' + error.message);
    });
}

// Populate Radarr server dropdown
function populateJellyseerrRadarrServers() {
    var serverSelect = document.getElementById('JellyseerrRadarrServer');
    serverSelect.innerHTML = '<option value="">Select a Radarr server...</option>';

    // Determine which server to select - prioritize saved config over API default
    var serverToSelect = null;
    if (JellyNextConfig.savedJellyseerrRadarrServerId !== null) {
        // User has a saved selection in config - prioritize it
        serverToSelect = JellyNextConfig.jellyseerrRadarrServers.find(function (s) {
            return s.id == JellyNextConfig.savedJellyseerrRadarrServerId;
        });
    }
    if (!serverToSelect) {
        // No saved selection or saved server not found - use default from API
        serverToSelect = JellyNextConfig.jellyseerrRadarrServers.find(function (s) { return s.isDefault; });
    }

    JellyNextConfig.jellyseerrRadarrServers.forEach(function (server) {
        var option = document.createElement('option');
        option.value = server.id;
        option.text = server.name + (server.isDefault ? ' (Default)' : '');
        if (serverToSelect && server.id === serverToSelect.id) {
            option.selected = true;
        }
        serverSelect.appendChild(option);
    });

    // Load profiles for selected server
    if (serverToSelect) {
        // Prioritize saved profile ID from config over server's active profile
        var profileIdToUse = JellyNextConfig.savedJellyseerrRadarrProfileId !== null
            ? JellyNextConfig.savedJellyseerrRadarrProfileId
            : serverToSelect.activeProfileId;
        loadJellyseerrRadarrProfiles(serverToSelect.id, profileIdToUse);
    }
}

// Populate Sonarr server dropdown
function populateJellyseerrSonarrServers() {
    var serverSelect = document.getElementById('JellyseerrSonarrServer');
    serverSelect.innerHTML = '<option value="">Select a Sonarr server...</option>';

    // Determine which server to select - prioritize saved config over API default
    var serverToSelect = null;
    if (JellyNextConfig.savedJellyseerrSonarrServerId !== null) {
        // User has a saved selection in config - prioritize it
        serverToSelect = JellyNextConfig.jellyseerrSonarrServers.find(function (s) {
            return s.id == JellyNextConfig.savedJellyseerrSonarrServerId;
        });
    }
    if (!serverToSelect) {
        // No saved selection or saved server not found - use default from API
        serverToSelect = JellyNextConfig.jellyseerrSonarrServers.find(function (s) { return s.isDefault; });
    }

    JellyNextConfig.jellyseerrSonarrServers.forEach(function (server) {
        var option = document.createElement('option');
        option.value = server.id;
        option.text = server.name + (server.isDefault ? ' (Default)' : '');
        if (serverToSelect && server.id === serverToSelect.id) {
            option.selected = true;
        }
        serverSelect.appendChild(option);
    });

    // Load profiles for selected server
    if (serverToSelect) {
        // Prioritize saved profile IDs from config over server's active profiles
        var profileIdToUse = JellyNextConfig.savedJellyseerrSonarrProfileId !== null
            ? JellyNextConfig.savedJellyseerrSonarrProfileId
            : serverToSelect.activeProfileId;
        var animeProfileIdToUse = JellyNextConfig.savedJellyseerrSonarrAnimeProfileId !== null
            ? JellyNextConfig.savedJellyseerrSonarrAnimeProfileId
            : serverToSelect.activeAnimeProfileId;
        loadJellyseerrSonarrProfiles(serverToSelect.id, profileIdToUse, animeProfileIdToUse);
    }
}

// Load Radarr profiles for a server
function loadJellyseerrRadarrProfiles(serverId, defaultProfileId) {
    var jellyseerrUrl = document.getElementById('JellyseerrUrl').value;
    var jellyseerrApiKey = document.getElementById('JellyseerrApiKey').value;
    var encodedUrl = encodeURIComponent(jellyseerrUrl);
    var encodedApiKey = encodeURIComponent(jellyseerrApiKey);

    ApiClient.fetch({
        type: 'GET',
        url: ApiClient.getUrl('JellyNext/Jellyseerr/Radarr/' + serverId + '/Profiles?jellyseerrUrl=' + encodedUrl + '&apiKey=' + encodedApiKey)
    }).then(function (response) {
        return response.json();
    }).then(function (profiles) {
        JellyNextConfig.jellyseerrRadarrProfiles = profiles || [];
        var profileSelect = document.getElementById('JellyseerrRadarrProfile');
        profileSelect.innerHTML = '<option value="">Select a quality profile...</option>';

        profiles.forEach(function (profile) {
            var option = document.createElement('option');
            option.value = profile.id;
            option.text = profile.name;
            if (profile.id === defaultProfileId) {
                option.selected = true;
            }
            profileSelect.appendChild(option);
        });
    }).catch(function (error) {
        console.error('Error loading Radarr profiles:', error);
    });
}

// Load Sonarr profiles for a server
function loadJellyseerrSonarrProfiles(serverId, defaultProfileId, defaultAnimeProfileId) {
    var jellyseerrUrl = document.getElementById('JellyseerrUrl').value;
    var jellyseerrApiKey = document.getElementById('JellyseerrApiKey').value;
    var encodedUrl = encodeURIComponent(jellyseerrUrl);
    var encodedApiKey = encodeURIComponent(jellyseerrApiKey);

    ApiClient.fetch({
        type: 'GET',
        url: ApiClient.getUrl('JellyNext/Jellyseerr/Sonarr/' + serverId + '/Profiles?jellyseerrUrl=' + encodedUrl + '&apiKey=' + encodedApiKey)
    }).then(function (response) {
        return response.json();
    }).then(function (profiles) {
        JellyNextConfig.jellyseerrSonarrProfiles = profiles || [];

        // Populate regular profile dropdown
        var profileSelect = document.getElementById('JellyseerrSonarrProfile');
        profileSelect.innerHTML = '<option value="">Select a quality profile...</option>';

        profiles.forEach(function (profile) {
            var option = document.createElement('option');
            option.value = profile.id;
            option.text = profile.name;
            if (profile.id === defaultProfileId) {
                option.selected = true;
            }
            profileSelect.appendChild(option);
        });

        // Populate anime profile dropdown
        var animeProfileSelect = document.getElementById('JellyseerrSonarrAnimeProfile');
        animeProfileSelect.innerHTML = '<option value="">Use same profile as regular TV shows</option>';

        profiles.forEach(function (profile) {
            var option = document.createElement('option');
            option.value = profile.id;
            option.text = profile.name;
            if (defaultAnimeProfileId && profile.id === defaultAnimeProfileId) {
                option.selected = true;
            }
            animeProfileSelect.appendChild(option);
        });
    }).catch(function (error) {
        console.error('Error loading Sonarr profiles:', error);
    });
}

// Populate Radarr quality profiles and root folders
function populateRadarrOptions() {
    var profileSelect = document.getElementById('RadarrQualityProfile');
    var folderSelect = document.getElementById('RadarrRootFolder');

    profileSelect.innerHTML = '<option value="">Select a quality profile...</option>';
    JellyNextConfig.radarrProfiles.forEach(function (profile) {
        var option = document.createElement('option');
        option.value = profile.id;
        option.textContent = profile.name;
        profileSelect.appendChild(option);
    });

    folderSelect.innerHTML = '<option value="">Select a root folder...</option>';
    JellyNextConfig.radarrFolders.forEach(function (folder) {
        var option = document.createElement('option');
        option.value = folder.path;
        option.textContent = folder.path + ' (' + formatBytes(folder.freeSpace) + ' free)';
        folderSelect.appendChild(option);
    });

    // Try to restore previously selected values
    ApiClient.getPluginConfiguration(JellyNextConfig.pluginUniqueId).then(function (config) {
        if (config.RadarrQualityProfileId) {
            profileSelect.value = config.RadarrQualityProfileId;
        }
        if (config.RadarrRootFolderPath) {
            folderSelect.value = config.RadarrRootFolderPath;
        }
    });
}

// Populate Sonarr quality profiles and root folders
function populateSonarrOptions() {
    var profileSelect = document.getElementById('SonarrQualityProfile');
    var folderSelect = document.getElementById('SonarrRootFolder');
    var animeFolderSelect = document.getElementById('SonarrAnimeRootFolder');

    profileSelect.innerHTML = '<option value="">Select a quality profile...</option>';
    JellyNextConfig.sonarrProfiles.forEach(function (profile) {
        var option = document.createElement('option');
        option.value = profile.id;
        option.textContent = profile.name;
        profileSelect.appendChild(option);
    });

    folderSelect.innerHTML = '<option value="">Select a root folder...</option>';
    JellyNextConfig.sonarrFolders.forEach(function (folder) {
        var option = document.createElement('option');
        option.value = folder.path;
        option.textContent = folder.path + ' (' + formatBytes(folder.freeSpace) + ' free)';
        folderSelect.appendChild(option);
    });

    animeFolderSelect.innerHTML = '<option value="">Same as regular shows...</option>';
    JellyNextConfig.sonarrFolders.forEach(function (folder) {
        var option = document.createElement('option');
        option.value = folder.path;
        option.textContent = folder.path + ' (' + formatBytes(folder.freeSpace) + ' free)';
        animeFolderSelect.appendChild(option);
    });

    // Try to restore previously selected values
    ApiClient.getPluginConfiguration(JellyNextConfig.pluginUniqueId).then(function (config) {
        if (config.SonarrQualityProfileId) {
            profileSelect.value = config.SonarrQualityProfileId;
        }
        if (config.SonarrRootFolderPath) {
            folderSelect.value = config.SonarrRootFolderPath;
        }
        if (config.SonarrAnimeRootFolderPath) {
            animeFolderSelect.value = config.SonarrAnimeRootFolderPath;
        }
    });
}

// Format bytes to human-readable size
function formatBytes(bytes) {
    if (bytes === 0) return '0 B';
    var k = 1024;
    var sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    var i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

// Default payload templates
var defaultMoviePayload = `{
  "tmdbId": "{tmdbId}",
  "imdbId": "{imdbId}",
  "title": "{title}",
  "year": "{year}",
  "jellyfinUserId": "{jellyfinUserId}"
}`;

var defaultShowPayload = `{
  "tvdbId": "{tvdbId}",
  "tmdbId": "{tmdbId}",
  "imdbId": "{imdbId}",
  "title": "{title}",
  "year": "{year}",
  "seasonNumber": {seasonNumber},
  "isAnime": {isAnime},
  "jellyfinUserId": "{jellyfinUserId}"
}`;

// Dynamic header management
var movieHeadersData = [];
var showHeadersData = [];

function createHeaderRow(name, value, type, index) {
    var row = document.createElement('div');
    row.style.cssText = 'display: grid; grid-template-columns: 1fr 2fr auto; gap: 0.5em; align-items: center; padding: 0.5em; background-color: rgba(255, 255, 255, 0.03); border-radius: 4px;';

    // Create name input
    var nameInput = document.createElement('input');
    nameInput.type = 'text';
    nameInput.placeholder = 'Header Name';
    nameInput.value = name || '';
    nameInput.setAttribute('data-index', index);
    nameInput.setAttribute('data-type', type);
    nameInput.setAttribute('data-field', 'name');
    nameInput.style.cssText = 'padding: 0.5em; font-size: 0.9em;';

    // Create value input
    var valueInput = document.createElement('input');
    valueInput.type = 'text';
    valueInput.placeholder = 'Header Value';
    valueInput.value = value || '';
    valueInput.setAttribute('data-index', index);
    valueInput.setAttribute('data-type', type);
    valueInput.setAttribute('data-field', 'value');
    valueInput.style.cssText = 'padding: 0.5em; font-size: 0.9em;';

    // Create remove button
    var removeBtn = document.createElement('button');
    removeBtn.type = 'button';
    removeBtn.className = 'remove-header-btn';
    removeBtn.setAttribute('data-index', index);
    removeBtn.setAttribute('data-type', type);
    removeBtn.textContent = '×';
    removeBtn.style.cssText = 'padding: 0.5em 0.75em; background-color: rgba(204, 51, 51, 0.2); border: 1px solid rgba(204, 51, 51, 0.5); border-radius: 4px; cursor: pointer; color: #cc3333; font-size: 0.9em;';

    row.appendChild(nameInput);
    row.appendChild(valueInput);
    row.appendChild(removeBtn);

    return row;
}

function renderMovieHeaders() {
    var container = document.getElementById('MovieHeadersList');
    container.innerHTML = '';
    if (movieHeadersData.length === 0) {
        container.innerHTML = '<div style="padding: 1em; text-align: center; opacity: 0.5; font-size: 0.9em;">No custom headers. Click "+ Add Header" to add one.</div>';
    } else {
        movieHeadersData.forEach(function(header, index) {
            container.appendChild(createHeaderRow(header.name, header.value, 'movie', index));
        });
    }
}

function renderShowHeaders() {
    var container = document.getElementById('ShowHeadersList');
    container.innerHTML = '';
    if (showHeadersData.length === 0) {
        container.innerHTML = '<div style="padding: 1em; text-align: center; opacity: 0.5; font-size: 0.9em;">No custom headers. Click "+ Add Header" to add one.</div>';
    } else {
        showHeadersData.forEach(function(header, index) {
            container.appendChild(createHeaderRow(header.name, header.value, 'show', index));
        });
    }
}

// Setup all download-related event listeners
function setupDownloadEventListeners() {
    // Download integration radio buttons
    document.getElementById('DownloadIntegrationNative').addEventListener('change', function () {
        if (this.checked) {
            toggleDownloadIntegration(0);
        }
    });

    document.getElementById('DownloadIntegrationJellyseerr').addEventListener('change', function () {
        if (this.checked) {
            toggleDownloadIntegration(1);
        }
    });

    document.getElementById('DownloadIntegrationWebhook').addEventListener('change', function () {
        if (this.checked) {
            toggleDownloadIntegration(2);
        }
    });

    // Jellyseerr default config checkboxes
    document.getElementById('UseJellyseerrRadarrDefaults').addEventListener('change', function () {
        toggleJellyseerrRadarrManualConfig();
    });

    document.getElementById('UseJellyseerrSonarrDefaults').addEventListener('change', function () {
        toggleJellyseerrSonarrManualConfig();
    });

    // Test Jellyseerr connection
    document.getElementById('TestJellyseerrBtn').addEventListener('click', function () {
        var jellyseerrUrl = document.getElementById('JellyseerrUrl').value;
        var jellyseerrApiKey = document.getElementById('JellyseerrApiKey').value;

        if (!jellyseerrUrl || !jellyseerrApiKey) {
            Dashboard.alert('Please enter Jellyseerr URL and API Key first');
            return;
        }

        Dashboard.showLoadingMsg();
        var statusDiv = document.getElementById('JellyseerrConnectionStatus');
        statusDiv.style.display = 'none';

        var encodedUrl = encodeURIComponent(jellyseerrUrl);
        var encodedApiKey = encodeURIComponent(jellyseerrApiKey);

        ApiClient.fetch({
            type: 'GET',
            url: ApiClient.getUrl('JellyNext/Jellyseerr/TestConnection?jellyseerrUrl=' + encodedUrl + '&apiKey=' + encodedApiKey)
        }).then(function (response) {
            return response.json();
        }).then(function (result) {
            statusDiv.style.display = 'block';

            if (result.success || result.Success) {
                var version = result.version || result.Version || 'Unknown';
                statusDiv.innerHTML = '<div class="fieldDescription" style="color: #52B54B;">✓ Successfully connected to Jellyseerr v' + version + '</div>';

                // Load servers and profiles
                loadJellyseerrServers(jellyseerrUrl, jellyseerrApiKey);
            } else {
                Dashboard.hideLoadingMsg();
                var errorMsg = result.errorMessage || result.ErrorMessage || 'Unknown error';
                statusDiv.innerHTML = '<div class="fieldDescription" style="color: #cc3333;">✗ Connection failed: ' + errorMsg + '</div>';
            }
        }).catch(function (error) {
            Dashboard.hideLoadingMsg();
            statusDiv.style.display = 'block';
            statusDiv.innerHTML = '<div class="fieldDescription" style="color: #cc3333;">✗ Connection failed: ' + error.message + '</div>';
        });
    });

    // Jellyseerr Radarr server selection change
    document.getElementById('JellyseerrRadarrServer').addEventListener('change', function () {
        var serverId = parseInt(this.value);
        if (!serverId) return;

        var server = JellyNextConfig.jellyseerrRadarrServers.find(function (s) { return s.id === serverId; });
        if (server) {
            loadJellyseerrRadarrProfiles(serverId, server.activeProfileId);
        }
    });

    // Jellyseerr Sonarr server selection change
    document.getElementById('JellyseerrSonarrServer').addEventListener('change', function () {
        var serverId = parseInt(this.value);
        if (!serverId) return;

        var server = JellyNextConfig.jellyseerrSonarrServers.find(function (s) { return s.id === serverId; });
        if (server) {
            loadJellyseerrSonarrProfiles(serverId, server.activeProfileId, server.activeAnimeProfileId);
        }
    });

    // Test Radarr connection
    document.getElementById('TestRadarrBtn').addEventListener('click', function () {
        var radarrUrl = document.getElementById('RadarrUrl').value;
        var radarrApiKey = document.getElementById('RadarrApiKey').value;

        if (!radarrUrl || !radarrApiKey) {
            Dashboard.alert('Please enter Radarr URL and API Key first');
            return;
        }

        Dashboard.showLoadingMsg();
        var statusDiv = document.getElementById('RadarrConnectionStatus');
        var optionsSection = document.getElementById('RadarrOptionsSection');
        statusDiv.style.display = 'none';
        optionsSection.style.display = 'none';

        var encodedUrl = encodeURIComponent(radarrUrl);
        var encodedApiKey = encodeURIComponent(radarrApiKey);

        ApiClient.fetch({
            type: 'GET',
            url: ApiClient.getUrl('JellyNext/Radarr/TestConnection?radarrUrl=' + encodedUrl + '&apiKey=' + encodedApiKey)
        }).then(function (response) {
            return response.json();
        }).then(function (result) {
            Dashboard.hideLoadingMsg();
            statusDiv.style.display = 'block';

            if (result.Success) {
                statusDiv.innerHTML = '<div class="fieldDescription" style="color: #52B54B;">✓ Successfully connected to Radarr v' + result.Version + '</div>';

                JellyNextConfig.radarrProfiles = result.QualityProfiles || [];
                JellyNextConfig.radarrFolders = result.RootFolders || [];

                populateRadarrOptions();
                optionsSection.style.display = 'block';
            } else {
                statusDiv.innerHTML = '<div class="fieldDescription" style="color: #cc3333;">✗ Connection failed: ' + (result.ErrorMessage || 'Unknown error') + '</div>';
                optionsSection.style.display = 'none';
            }
        }).catch(function (error) {
            Dashboard.hideLoadingMsg();
            statusDiv.style.display = 'block';
            statusDiv.innerHTML = '<div class="fieldDescription" style="color: #cc3333;">✗ Connection failed: ' + error.message + '</div>';
            optionsSection.style.display = 'none';
        });
    });

    // Test Sonarr connection
    document.getElementById('TestSonarrBtn').addEventListener('click', function () {
        var sonarrUrl = document.getElementById('SonarrUrl').value;
        var sonarrApiKey = document.getElementById('SonarrApiKey').value;

        if (!sonarrUrl || !sonarrApiKey) {
            Dashboard.alert('Please enter Sonarr URL and API Key first');
            return;
        }

        Dashboard.showLoadingMsg();
        var statusDiv = document.getElementById('SonarrConnectionStatus');
        var optionsSection = document.getElementById('SonarrOptionsSection');
        statusDiv.style.display = 'none';
        optionsSection.style.display = 'none';

        var encodedUrl = encodeURIComponent(sonarrUrl);
        var encodedApiKey = encodeURIComponent(sonarrApiKey);

        ApiClient.fetch({
            type: 'GET',
            url: ApiClient.getUrl('JellyNext/Sonarr/TestConnection?sonarrUrl=' + encodedUrl + '&apiKey=' + encodedApiKey)
        }).then(function (response) {
            return response.json();
        }).then(function (result) {
            Dashboard.hideLoadingMsg();
            statusDiv.style.display = 'block';

            if (result.Success) {
                statusDiv.innerHTML = '<div class="fieldDescription" style="color: #52B54B;">✓ Successfully connected to Sonarr v' + result.Version + '</div>';

                JellyNextConfig.sonarrProfiles = result.QualityProfiles || [];
                JellyNextConfig.sonarrFolders = result.RootFolders || [];

                populateSonarrOptions();
                optionsSection.style.display = 'block';
            } else {
                statusDiv.innerHTML = '<div class="fieldDescription" style="color: #cc3333;">✗ Connection failed: ' + (result.ErrorMessage || 'Unknown error') + '</div>';
                optionsSection.style.display = 'none';
            }
        }).catch(function (error) {
            Dashboard.hideLoadingMsg();
            statusDiv.style.display = 'block';
            statusDiv.innerHTML = '<div class="fieldDescription" style="color: #cc3333;">✗ Connection failed: ' + error.message + '</div>';
            optionsSection.style.display = 'none';
        });
    });

    // Add header buttons
    document.getElementById('AddMovieHeaderBtn').addEventListener('click', function() {
        movieHeadersData.push({ name: '', value: '' });
        renderMovieHeaders();
    });

    document.getElementById('AddShowHeaderBtn').addEventListener('click', function() {
        showHeadersData.push({ name: '', value: '' });
        renderShowHeaders();
    });

    // Event delegation for header input changes
    document.addEventListener('input', function(e) {
        var target = e.target;
        if (target.hasAttribute('data-type') && target.hasAttribute('data-field')) {
            var index = parseInt(target.getAttribute('data-index'));
            var type = target.getAttribute('data-type');
            var field = target.getAttribute('data-field');
            var dataArray = type === 'movie' ? movieHeadersData : showHeadersData;
            if (dataArray[index]) {
                dataArray[index][field] = target.value;
            }
        }
    });

    // Event delegation for header removal
    document.addEventListener('click', function(e) {
        if (e.target.classList.contains('remove-header-btn')) {
            var index = parseInt(e.target.getAttribute('data-index'));
            var type = e.target.getAttribute('data-type');
            if (type === 'movie') {
                movieHeadersData.splice(index, 1);
                renderMovieHeaders();
            } else {
                showHeadersData.splice(index, 1);
                renderShowHeaders();
            }
        }
    });

    // Placeholder button clicks
    document.addEventListener('click', function(e) {
        if (e.target.classList.contains('placeholder-btn')) {
            var placeholder = e.target.getAttribute('data-placeholder');
            var isMovie = e.target.classList.contains('movie-placeholder');
            var textarea = document.getElementById(isMovie ? 'WebhookMoviePayload' : 'WebhookShowPayload');

            // Insert at cursor position
            var startPos = textarea.selectionStart;
            var endPos = textarea.selectionEnd;
            var textBefore = textarea.value.substring(0, startPos);
            var textAfter = textarea.value.substring(endPos);

            textarea.value = textBefore + placeholder + textAfter;
            textarea.selectionStart = textarea.selectionEnd = startPos + placeholder.length;
            textarea.focus();
        }
    });

    // Reset movie payload button
    document.getElementById('ResetMoviePayloadBtn').addEventListener('click', function () {
        document.getElementById('WebhookMoviePayload').value = defaultMoviePayload;
    });

    // Reset show payload button
    document.getElementById('ResetShowPayloadBtn').addEventListener('click', function () {
        document.getElementById('WebhookShowPayload').value = defaultShowPayload;
    });
}

// Load configuration for Downloads tab
function loadDownloadsConfig(config) {
    // Download integration settings
    var downloadIntegration = config.DownloadIntegration !== undefined ? config.DownloadIntegration : 0;
    if (downloadIntegration === 0 || downloadIntegration === "Native") {
        downloadIntegration = 0;
    } else if (downloadIntegration === "Jellyseerr") {
        downloadIntegration = 1;
    } else if (downloadIntegration === "Webhook") {
        downloadIntegration = 2;
    }
    if (downloadIntegration === 0 || downloadIntegration === '0') {
        document.getElementById('DownloadIntegrationNative').checked = true;
    } else if (downloadIntegration === 1 || downloadIntegration === '1') {
        document.getElementById('DownloadIntegrationJellyseerr').checked = true;
    } else {
        document.getElementById('DownloadIntegrationWebhook').checked = true;
    }
    toggleDownloadIntegration(downloadIntegration);

    // Webhook settings
    document.getElementById('WebhookMethod').value = config.WebhookMethod || 'POST';
    document.getElementById('WebhookMovieUrl').value = config.WebhookMovieUrl || '';
    document.getElementById('WebhookShowUrl').value = config.WebhookShowUrl || '';

    // Load webhook headers into dynamic arrays
    movieHeadersData = (config.WebhookMovieHeaders || []).map(h => ({
        name: h.Name || '',
        value: h.Value || ''
    }));
    showHeadersData = (config.WebhookShowHeaders || []).map(h => ({
        name: h.Name || '',
        value: h.Value || ''
    }));

    // Render headers
    renderMovieHeaders();
    renderShowHeaders();

    // Load webhook payloads (use defaults if not set)
    document.getElementById('WebhookMoviePayload').value = config.WebhookMoviePayload || defaultMoviePayload;
    document.getElementById('WebhookShowPayload').value = config.WebhookShowPayload || defaultShowPayload;

    document.getElementById('JellyseerrUrl').value = config.JellyseerrUrl || '';
    document.getElementById('JellyseerrApiKey').value = config.JellyseerrApiKey || '';

    // Load Jellyseerr default config checkboxes
    document.getElementById('UseJellyseerrRadarrDefaults').checked = config.UseJellyseerrRadarrDefaults !== false;
    document.getElementById('UseJellyseerrSonarrDefaults').checked = config.UseJellyseerrSonarrDefaults !== false;
    toggleJellyseerrRadarrManualConfig();
    toggleJellyseerrSonarrManualConfig();

    // Store Jellyseerr server/profile selections in memory for later use
    JellyNextConfig.savedJellyseerrRadarrServerId = config.JellyseerrRadarrServerId !== null && config.JellyseerrRadarrServerId !== undefined
        ? config.JellyseerrRadarrServerId
        : null;
    JellyNextConfig.savedJellyseerrRadarrProfileId = config.JellyseerrRadarrProfileId !== null && config.JellyseerrRadarrProfileId !== undefined
        ? config.JellyseerrRadarrProfileId
        : null;
    JellyNextConfig.savedJellyseerrSonarrServerId = config.JellyseerrSonarrServerId !== null && config.JellyseerrSonarrServerId !== undefined
        ? config.JellyseerrSonarrServerId
        : null;
    JellyNextConfig.savedJellyseerrSonarrProfileId = config.JellyseerrSonarrProfileId !== null && config.JellyseerrSonarrProfileId !== undefined
        ? config.JellyseerrSonarrProfileId
        : null;
    JellyNextConfig.savedJellyseerrSonarrAnimeProfileId = config.JellyseerrSonarrAnimeProfileId !== null && config.JellyseerrSonarrAnimeProfileId !== undefined
        ? config.JellyseerrSonarrAnimeProfileId
        : null;

    // Also set the dropdown values (for initial display before Test Connection is clicked)
    if (JellyNextConfig.savedJellyseerrRadarrServerId !== null) {
        document.getElementById('JellyseerrRadarrServer').value = JellyNextConfig.savedJellyseerrRadarrServerId;
    }
    if (JellyNextConfig.savedJellyseerrRadarrProfileId !== null) {
        document.getElementById('JellyseerrRadarrProfile').value = JellyNextConfig.savedJellyseerrRadarrProfileId;
    }
    if (JellyNextConfig.savedJellyseerrSonarrServerId !== null) {
        document.getElementById('JellyseerrSonarrServer').value = JellyNextConfig.savedJellyseerrSonarrServerId;
    }
    if (JellyNextConfig.savedJellyseerrSonarrProfileId !== null) {
        document.getElementById('JellyseerrSonarrProfile').value = JellyNextConfig.savedJellyseerrSonarrProfileId;
    }
    if (JellyNextConfig.savedJellyseerrSonarrAnimeProfileId !== null) {
        document.getElementById('JellyseerrSonarrAnimeProfile').value = JellyNextConfig.savedJellyseerrSonarrAnimeProfileId;
    }

    // Native integration settings
    document.getElementById('RadarrUrl').value = config.RadarrUrl || '';
    document.getElementById('RadarrApiKey').value = config.RadarrApiKey || '';
    document.getElementById('SonarrUrl').value = config.SonarrUrl || '';
    document.getElementById('SonarrApiKey').value = config.SonarrApiKey || '';
}

// Save configuration for Downloads tab
function saveDownloadsConfig(config) {
    // Download integration mode
    if (document.getElementById('DownloadIntegrationNative').checked) {
        config.DownloadIntegration = 0;
    } else if (document.getElementById('DownloadIntegrationJellyseerr').checked) {
        config.DownloadIntegration = 1;
    } else {
        config.DownloadIntegration = 2;
    }

    // Webhook settings
    config.WebhookMethod = document.getElementById('WebhookMethod').value;
    config.WebhookMovieUrl = document.getElementById('WebhookMovieUrl').value;
    config.WebhookShowUrl = document.getElementById('WebhookShowUrl').value;
    config.WebhookMoviePayload = document.getElementById('WebhookMoviePayload').value;
    config.WebhookShowPayload = document.getElementById('WebhookShowPayload').value;

    // Convert header arrays to config format
    config.WebhookMovieHeaders = movieHeadersData
        .filter(h => h.name && h.value)
        .map(h => ({ Name: h.name, Value: h.value }));
    config.WebhookShowHeaders = showHeadersData
        .filter(h => h.name && h.value)
        .map(h => ({ Name: h.name, Value: h.value }));

    // Jellyseerr settings
    config.JellyseerrUrl = document.getElementById('JellyseerrUrl').value;
    config.JellyseerrApiKey = document.getElementById('JellyseerrApiKey').value;
    config.UseJellyseerrRadarrDefaults = document.getElementById('UseJellyseerrRadarrDefaults').checked;
    config.UseJellyseerrSonarrDefaults = document.getElementById('UseJellyseerrSonarrDefaults').checked;

    // Only save server/profile selections if manual config is enabled
    if (!config.UseJellyseerrRadarrDefaults) {
        var radarrServerId = document.getElementById('JellyseerrRadarrServer').value;
        var radarrProfileId = document.getElementById('JellyseerrRadarrProfile').value;
        config.JellyseerrRadarrServerId = radarrServerId ? parseInt(radarrServerId) : null;
        config.JellyseerrRadarrProfileId = radarrProfileId ? parseInt(radarrProfileId) : null;
    } else {
        config.JellyseerrRadarrServerId = null;
        config.JellyseerrRadarrProfileId = null;
    }

    if (!config.UseJellyseerrSonarrDefaults) {
        var sonarrServerId = document.getElementById('JellyseerrSonarrServer').value;
        var sonarrProfileId = document.getElementById('JellyseerrSonarrProfile').value;
        var sonarrAnimeProfileId = document.getElementById('JellyseerrSonarrAnimeProfile').value;
        config.JellyseerrSonarrServerId = sonarrServerId ? parseInt(sonarrServerId) : null;
        config.JellyseerrSonarrProfileId = sonarrProfileId ? parseInt(sonarrProfileId) : null;
        config.JellyseerrSonarrAnimeProfileId = sonarrAnimeProfileId ? parseInt(sonarrAnimeProfileId) : null;
    } else {
        config.JellyseerrSonarrServerId = null;
        config.JellyseerrSonarrProfileId = null;
        config.JellyseerrSonarrAnimeProfileId = null;
    }

    // Native integration settings
    config.RadarrUrl = document.getElementById('RadarrUrl').value;
    config.RadarrApiKey = document.getElementById('RadarrApiKey').value;

    var radarrQualityProfile = document.getElementById('RadarrQualityProfile').value;
    config.RadarrQualityProfileId = radarrQualityProfile ? parseInt(radarrQualityProfile) : null;
    config.RadarrRootFolderPath = document.getElementById('RadarrRootFolder').value || null;

    config.SonarrUrl = document.getElementById('SonarrUrl').value;
    config.SonarrApiKey = document.getElementById('SonarrApiKey').value;

    var sonarrQualityProfile = document.getElementById('SonarrQualityProfile').value;
    config.SonarrQualityProfileId = sonarrQualityProfile ? parseInt(sonarrQualityProfile) : null;
    config.SonarrRootFolderPath = document.getElementById('SonarrRootFolder').value || null;
    config.SonarrAnimeRootFolderPath = document.getElementById('SonarrAnimeRootFolder').value || null;
}
