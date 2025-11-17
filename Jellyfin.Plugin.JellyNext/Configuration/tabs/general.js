// General tab initialization and logic

function initGeneralTab() {
    // General tab has no specific initialization logic
    // All inputs are handled by the global form load/save logic
    console.log('General tab initialized');
}

function loadGeneralSettings(config) {
    document.getElementById('CacheExpirationHours').value = config.CacheExpirationHours || 6;
    document.getElementById('UseShortDummyVideo').checked = config.UseShortDummyVideo !== false;
    document.getElementById('PlaybackStopDelaySeconds').value = config.PlaybackStopDelaySeconds || 2;
}

function saveGeneralSettings(config) {
    config.CacheExpirationHours = parseInt(document.getElementById('CacheExpirationHours').value, 10);
    config.UseShortDummyVideo = document.getElementById('UseShortDummyVideo').checked;
    config.PlaybackStopDelaySeconds = parseInt(document.getElementById('PlaybackStopDelaySeconds').value, 10);
}
