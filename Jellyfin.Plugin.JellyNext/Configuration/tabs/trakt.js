// Trakt tab initialization and logic

function initTraktTab() {
    // Set up event listeners for Trakt tab
    setupTraktEventListeners();
    console.log('Trakt tab initialized');
}

function setupTraktEventListeners() {
    // User selection change
    document.getElementById('UserSelector').addEventListener('change', function (e) {
        var userGuid = e.target.value;
        if (JellyNextConfig.authCheckInterval) {
            clearInterval(JellyNextConfig.authCheckInterval);
        }
        checkAuthorizationStatus(userGuid);
    });

    // Start OAuth authorization
    document.getElementById('AuthorizeBtn').addEventListener('click', function () {
        if (!JellyNextConfig.currentUserGuid) {
            Dashboard.alert('Please select a user first');
            return;
        }

        Dashboard.showLoadingMsg();

        ApiClient.fetch({
            type: 'POST',
            url: ApiClient.getUrl('JellyNext/Trakt/Users/' + JellyNextConfig.currentUserGuid + '/Authorize')
        }).then(function (response) {
            return response.json();
        }).then(function (result) {
            Dashboard.hideLoadingMsg();
            showAuthorizingState(result.userCode);

            // Poll for authorization completion
            JellyNextConfig.authCheckInterval = setInterval(function () {
                checkAuthorizationCompletion();
            }, 3000); // Check every 3 seconds
        }).catch(function (error) {
            Dashboard.hideLoadingMsg();
            console.error('Error starting authorization:', error);
            Dashboard.alert('Failed to start authorization. Please try again.');
        });
    });

    // Deauthorize user
    document.getElementById('DeauthorizeBtn').addEventListener('click', function () {
        if (!JellyNextConfig.currentUserGuid) {
            return;
        }

        if (!confirm('Are you sure you want to unlink this Trakt account?')) {
            return;
        }

        Dashboard.showLoadingMsg();

        ApiClient.fetch({
            type: 'POST',
            url: ApiClient.getUrl('JellyNext/Trakt/Users/' + JellyNextConfig.currentUserGuid + '/Deauthorize')
        }).then(function (response) {
            return response.json();
        }).then(function (result) {
            Dashboard.hideLoadingMsg();
            Dashboard.alert('Successfully unlinked Trakt account');
            showNotAuthorizedState();
        }).catch(function (error) {
            Dashboard.hideLoadingMsg();
            console.error('Error deauthorizing:', error);
            Dashboard.alert('Failed to unlink Trakt account');
        });
    });
}

// Check authorization status for selected user
function checkAuthorizationStatus(userGuid) {
    if (!userGuid) {
        document.getElementById('AuthorizationStatus').style.display = 'none';
        return;
    }

    JellyNextConfig.currentUserGuid = userGuid;
    document.getElementById('AuthorizationStatus').style.display = 'block';

    ApiClient.fetch({
        type: 'GET',
        url: ApiClient.getUrl('JellyNext/Trakt/Users/' + userGuid + '/AuthorizationStatus')
    }).then(function (response) {
        return response.json();
    }).then(function (status) {
        if (status.isAuthorized) {
            showAuthorizedState();
        } else {
            showNotAuthorizedState();
        }
    }).catch(function (error) {
        console.error('Error checking authorization status:', error);
        showNotAuthorizedState();
    });
}

// Show not authorized state
function showNotAuthorizedState() {
    document.getElementById('NotAuthorizedSection').style.display = 'block';
    document.getElementById('AuthorizedSection').style.display = 'none';
    document.getElementById('AuthorizingSection').style.display = 'none';
}

// Show authorized state
function showAuthorizedState() {
    document.getElementById('NotAuthorizedSection').style.display = 'none';
    document.getElementById('AuthorizedSection').style.display = 'block';
    document.getElementById('AuthorizingSection').style.display = 'none';
    loadUserSettings();
}

// Load user-specific settings
function loadUserSettings() {
    if (!JellyNextConfig.currentUserGuid) {
        return;
    }

    ApiClient.fetch({
        type: 'GET',
        url: ApiClient.getUrl('JellyNext/Trakt/Users/' + JellyNextConfig.currentUserGuid + '/Settings')
    }).then(function (response) {
        return response.json();
    }).then(function (settings) {
        document.getElementById('UserSyncMovieRecommendations').checked = settings.syncMovieRecommendations !== false;
        document.getElementById('UserSyncShowRecommendations').checked = settings.syncShowRecommendations !== false;
        document.getElementById('UserSyncNextSeasons').checked = settings.syncNextSeasons !== false;
        document.getElementById('UserIgnoreCollected').checked = settings.ignoreCollected !== false;
        document.getElementById('UserIgnoreWatchlisted').checked = settings.ignoreWatchlisted === true;
        document.getElementById('UserLimitShowsToSeasonOne').checked = settings.limitShowsToSeasonOne !== false;
        document.getElementById('UserMovieRecommendationsLimit').value = settings.movieRecommendationsLimit || 50;
        document.getElementById('UserShowRecommendationsLimit').value = settings.showRecommendationsLimit || 50;
    }).catch(function (error) {
        console.error('Error loading user settings:', error);
    });
}

// Show authorizing state
function showAuthorizingState(userCode) {
    document.getElementById('NotAuthorizedSection').style.display = 'none';
    document.getElementById('AuthorizedSection').style.display = 'none';
    document.getElementById('AuthorizingSection').style.display = 'block';
    document.getElementById('UserCodeDisplay').textContent = userCode;
}

// Check if authorization is complete
function checkAuthorizationCompletion() {
    ApiClient.fetch({
        type: 'GET',
        url: ApiClient.getUrl('JellyNext/Trakt/Users/' + JellyNextConfig.currentUserGuid + '/AuthorizationStatus')
    }).then(function (response) {
        return response.json();
    }).then(function (status) {
        if (status.isAuthorized) {
            clearInterval(JellyNextConfig.authCheckInterval);
            Dashboard.alert('Successfully linked Trakt account!');
            showAuthorizedState();
        }
    }).catch(function (error) {
        console.error('Error checking authorization completion:', error);
    });
}

// Save per-user Trakt settings
function saveUserTraktSettings(userGuid) {
    if (!userGuid) {
        return Promise.resolve();
    }

    var userSettings = {
        syncMovieRecommendations: document.getElementById('UserSyncMovieRecommendations').checked,
        syncShowRecommendations: document.getElementById('UserSyncShowRecommendations').checked,
        syncNextSeasons: document.getElementById('UserSyncNextSeasons').checked,
        ignoreCollected: document.getElementById('UserIgnoreCollected').checked,
        ignoreWatchlisted: document.getElementById('UserIgnoreWatchlisted').checked,
        limitShowsToSeasonOne: document.getElementById('UserLimitShowsToSeasonOne').checked,
        movieRecommendationsLimit: parseInt(document.getElementById('UserMovieRecommendationsLimit').value, 10),
        showRecommendationsLimit: parseInt(document.getElementById('UserShowRecommendationsLimit').value, 10)
    };

    return ApiClient.fetch({
        type: 'POST',
        url: ApiClient.getUrl('JellyNext/Trakt/Users/' + userGuid + '/Settings'),
        data: JSON.stringify(userSettings),
        contentType: 'application/json'
    });
}
