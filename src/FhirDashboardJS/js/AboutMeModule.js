class AboutMeModule {
    constructor(anchor, configAuth) {
        this.anchor = anchor;
        this.configAuth = configAuth;
    }

    render() {
        var aboutMe = this;
        aboutMe.anchor.html('Loading...');
        aboutMe.configAuth.getAboutMeInfo(function (userId, fhirServerUrl, accessToken) {
            const markup = `
                <h2>Information about the signed in user</h2>
                <div class="row"><p>UPN: ${userId}</p></div>
                <div class="row"><p>FHIR Server URL: ${fhirServerUrl}</p></div>
                <div class="row">
                    Access Token:
                    <form>
                        <textarea id="token" style="width: 20cm; height: 5cm;">${accessToken}</textarea>
                    </form>
                </div>
            `;

            aboutMe.anchor.html(markup);

        });
    }
}