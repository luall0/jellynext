// Trending tab initialization and logic

function initTrendingTab() {
    // Set up event listeners for Trending tab
    setupTrendingEventListeners();
    console.log('Trending tab initialized');
}

function setupTrendingEventListeners() {
    // Trending movies enabled checkbox
    document.getElementById('TrendingMoviesEnabled').addEventListener('change', function () {
        updateTrendingOptionsVisibility();
    });
}

// Update trending movies options visibility based on enabled checkbox
function updateTrendingOptionsVisibility() {
    var enabled = document.getElementById('TrendingMoviesEnabled').checked;
    document.getElementById('TrendingMoviesOptions').style.display = enabled ? 'block' : 'none';
}

function loadTrendingSettings(config) {
    var trendingEnabled = config.TrendingMoviesEnabled === true;
    document.getElementById('TrendingMoviesEnabled').checked = trendingEnabled;
    document.getElementById('TrendingMoviesLimit').value = config.TrendingMoviesLimit || 50;

    // Set trending movies user if available
    if (config.TrendingMoviesUserId) {
        document.getElementById('TrendingMoviesUser').value = config.TrendingMoviesUserId;
    }

    // Update visibility based on enabled state
    updateTrendingOptionsVisibility();
}

function saveTrendingSettings(config) {
    config.TrendingMoviesEnabled = document.getElementById('TrendingMoviesEnabled').checked;
    config.TrendingMoviesLimit = parseInt(document.getElementById('TrendingMoviesLimit').value, 10) || 50;

    var trendingUserId = document.getElementById('TrendingMoviesUser').value;
    config.TrendingMoviesUserId = trendingUserId || null;
}
