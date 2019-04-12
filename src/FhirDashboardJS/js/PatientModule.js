class PatientModule
{
    constructor(anchor, configAuth)
    {
        this.anchor = anchor;
        this.configAuth = configAuth;
    }

    renderPatientList()
    {
        var anchorElement = this.anchor;
        anchorElement.html('Loading...');

        configAuth.getFhirServerAccessInfo(function (fhirServerUrl, accessToken) {
            //Print the token
            $("#token").text(accessToken);

            //Get all patients
            $.ajax(
                {
                    type: 'GET',
                    url: fhirServerUrl + '/Patient',
                    beforeSend: function (xhdr) {
                        xhdr.setRequestHeader('Authorization', 'Bearer ' + accessToken);
                    },
                    success: function (result, status) {
                        //Create a list of patients
                        var patientListHtml = '<ol>';
                        $.each(result.entry, function (index, val) {
                            patientListHtml += '<li>' + val.resource.name[0].family + ', ' + val.resource.name[0].given + ' (' + val.resource.id + ')';
                        })
                        patientListHtml += '</ol>';
                        anchorElement.html(patientListHtml);
                    }
                }
            );
        });
    }
}