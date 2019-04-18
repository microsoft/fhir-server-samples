class ConfigAuth {
    constructor() {
    }

    getFhirServerUrl(callback) {
        var authConfig = this;

        if (authConfig.fhirServerUrl) {
            callback(authConfig.fhirServerUrl);
        }
        else {
            $.getJSON('/config.json', function (data, status) {
                authConfig.fhirServerUrl = data.FhirServerUrl;
                callback(authConfig.fhirServerUrl);
            });
        }
    }

    getSmartOnFhirApps(callback) {
        var authConfig = this;

        authConfig.getFhirServerUrl(function (fhirServerUrl) {
            if (authConfig.smartOnFhirApps) {
                callback(fhirServerUrl, authConfig.smartOnFhirApps);
            }
            else {
                $.getJSON('/config.json', function (data, status) {
                    authConfig.smartOnFhirApps = data.SmartOnFhirApps;
                    callback(fhirServerUrl, authConfig.smartOnFhirApps);
                });
            }
        });
    }


    getAccessToken(callback) {
        var authConfig = this;
        if (authConfig.authInfo && (new Date(authConfig.authInfo[0].expires_on) > new Date(Date.now() + 60000))) {
            callback(authConfig.authInfo[0].access_token);
        }
        else {
            authConfig.updateAuthInfo(function() {
                callback(authConfig.authInfo[0].access_token);
            });
        }
    }

    updateAuthInfo(callback)
    {
        var authConfig = this;
        authConfig.refreshTokens(function () {
            $.getJSON('/.auth/me', function (authData, status) {
                authConfig.authInfo = authData;
                callback();
            });
        });
    }

    refreshTokens(callback) {
        //Don't attempt to refresh token locally
        if (window.location.hostname.indexOf("localhost") != -1) {
            callback();
            return;
        }

        let refreshUrl = "/.auth/refresh";
        $.ajax(refreshUrl)
            .done(function () {
                console.log("Token refresh completed successfully.");
                callback()
            })
            .fail(function () {
                console.log("Token refresh failed. See application logs for details.");
            });
    }

    getFhirServerAccessInfo(callback) {
        var authConfig = this;
        authConfig.getFhirServerUrl(function (fhirServerUrl) {
            authConfig.getAccessToken(function (accessToken) {
                callback(fhirServerUrl, accessToken);
            })
        });
    }

    getUserId(callback)
    {
        var authConfig = this;
        if (authConfig.authInfo)
        {
            callback(authConfig.authInfo[0].user_id);
        }
        else
        {
            authConfig.updateAuthInfo(function() {
                callback(authConfig.authInfo[0].user_id);
            });
        }
    }

    getAboutMeInfo(callback)
    {
        var configAuth = this;
        configAuth.getUserId(function(userId){
            configAuth.getFhirServerAccessInfo(function(fhirServerUrl, accessToken){
                callback(userId, fhirServerUrl, accessToken);
            });
        });
    }
}