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
                <table>
                    <tr>
                        <td>UPN: ${userId}</td>
                    </tr>
                    <tr>
                        <td>FHIR Server URL: ${fhirServerUrl}</td>
                    </tr>
                    <tr>
                        <td>
                            Access Token:<br>
                            <form>
                                <textarea id="token" style="width: 20cm; height: 5cm;">${accessToken}</textarea>
                            </form>
                        </td>
                    </tr>
                </table>
            `;

            aboutMe.anchor.html(markup);

        });
    }
}