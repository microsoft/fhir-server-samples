class PatientModule
{
    constructor(anchor, configAuthModule)
    {
        this.anchor = anchor;
        this.configAuth = configAuthModule;
    }

    renderPatient(patientId)
    {
        const markup = `
            <h2 id="patient-details-name"></h2>

            <div style="margin-bottom: 10px;">
                <div>
                    <b>DOB:</b> <div style="display: inline;" id="patient-details-dob"></div>
                </div>
                <div>
                    <b>Gender:</b> <div style="display: inline;" id="patient-details-gender"></div>
                </div>
            </div>

            <div id="accordion">
            <div class="card">
            <div class="card-header">
                <a class="collapsed card-link" data-toggle="collapse" href="#collapseConditions">
                Conditions <div style="display: inline;" id="patient-condition-count">0</div>
                </a>
            </div>
            <div id="collapseConditions" class="collapse show" data-parent="#accordion">
                <div class="card-body">
                <table id="patient-condition-table" class="table">
                    <thead>
                        <tr>
                                <th>
                                    Onset
                                </th>
                                <th>
                                    Description
                                </th>
                                <th>
                                    Status
                                </th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                    </tbody>
                </table>
                </div>
            </div>
            </div>
        
            <div class="card">
            <div class="card-header">
                <a class="card-link" data-toggle="collapse" href="#collapseEncounters">
                Encounters <div id="patient-encounter-count" style="display: inline;">0</div>
                </a>
            </div>
            <div id="collapseEncounters" class="collapse" data-parent="#accordion">
                <div class="card-body">
                <table id="patient-encounter-table" class="table">
                    <thead>
                        <tr>
                                <th>
                                    Date
                                </th>
                                <th>
                                    Type
                                </th>
                                <th>
                                    Id
                                </th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                    </tbody>
                </table>
                </div>
            </div>
            </div>
        
            <div class="card">
            <div class="card-header">
                <a class="collapsed card-link" data-toggle="collapse" href="#collapseObservations">
                Observations <div id="patient-observation-count" style="display: inline;">0</div>
                </a>
            </div>
            <div id="collapseObservations" class="collapse" data-parent="#accordion">
                <div class="card-body">
                <table id="patient-observation-table" class="table">
                    <thead>
                        <tr>
                                <th>
                                    Date
                                </th>
                                <th>
                                    Title
                                </th>
                                <th>
                                    Id
                                </th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                    </tbody>
                </table>
                </div>
            </div>
            </div>
        </div>
        `;

        this.anchor.html(markup);

        //Now start loading the patient details
        this.fetchPatientInfo(patientId);
        this.fetchResources('/Condition?patient=' + patientId, this.addPatientCondition);
        this.fetchResources('/Encounter?patient=' + patientId, this.addPatientEncounter);
        this.fetchResources('/Observation?patient=' + patientId, this.addPatientObservation);
    }

    fetchPatientInfo(patientId)
    {
        this.configAuth.getFhirServerAccessInfo(function (fhirServerUrl, accessToken) {
            var fullQuery = fhirServerUrl + '/Patient/' + patientId;
            $.ajax(
                {
                    type: 'GET',
                    url: fullQuery,
                    beforeSend: function (xhdr) {
                        xhdr.setRequestHeader('Authorization', 'Bearer ' + accessToken);
                    },
                    success: function(data, status) {
                        var patientName = document.getElementById('patient-details-name');
                        patientName.innerHTML = data.name[0].family + ', ' + data.name[0].given;
                        var patientDob = document.getElementById('patient-details-dob');
                        patientDob.innerHTML = data.birthDate;
                        var patientGender = document.getElementById('patient-details-gender');
                        patientGender.innerHTML = data.gender;
                    }
                }
            );
        });
    }

    fetchResources(queryUrl, resourceAddCallback)
    {
        this.configAuth.getFhirServerAccessInfo(function (fhirServerUrl, accessToken) {
            var fullQuery = queryUrl;
            if (fullQuery.indexOf("https://") == -1) {
                fullQuery = fhirServerUrl +  fullQuery;
            }
            $.ajax(
                {
                    type: 'GET',
                    url: fullQuery,
                    beforeSend: function (xhdr) {
                        xhdr.setRequestHeader('Authorization', 'Bearer ' + accessToken);
                    },
                    success: function(data, status) {
                        //Follow any next links
                        $.each(data.link, function(index, linkVal){
                            if (linkVal.relation == "next") {
                                patientsModule.fetchResources(linkVal.url, resourceAddCallback);
                            }
                        });
                        $.each(data.entry, function(index, resourceVal){
                            resourceAddCallback(resourceVal.resource);
                        });
                    }
                }
            );
        });

    }

    addPatientCondition(conditionResource)
    {
        var conditionTable = document.getElementById('patient-condition-table');
        var conditionCount = document.getElementById('patient-condition-count');
        var row = conditionTable.insertRow(-1);
        var onset = row.insertCell(0);
        onset.innerText = conditionResource.onsetDateTime;
        var description = row.insertCell(1);
        description.innerText = conditionResource.code.text;
        var status = row.insertCell(2);
        status.innerText = conditionResource.clinicalStatus;
        conditionCount.innerText = Number(conditionCount.innerText) + 1;
    }

    addPatientEncounter(resource)
    {
        var table = document.getElementById('patient-encounter-table');
        var count = document.getElementById('patient-encounter-count');
        var row = table.insertRow(-1);
        var date = row.insertCell(0);
        date.innerText = resource.period.start;
        var title = row.insertCell(1);
        title.innerText = resource.type[0].text;
        var id = row.insertCell(2);
        id.innerText = resource.id;
        count.innerText = Number(count.innerText) + 1;
    }

    addPatientObservation(resource)
    {
        var table = document.getElementById('patient-observation-table');
        var count = document.getElementById('patient-observation-count');
        var row = table.insertRow(-1);
        var date = row.insertCell(0);
        date.innerText = resource.issued;
        var title = row.insertCell(1);
        title.innerText = resource.code.text;
        var id = row.insertCell(2);
        id.innerText = resource.id;
        count.innerText = Number(count.innerText) + 1;
    }

    renderPatientList()
    {
        var anchorElement = this.anchor;

        var markup = `
        <table id="patient-list-table" class="table">
            <thead>
                <tr>
                        <th>
                            Family Name
                        </th>
                        <th>
                            Given Name
                        </th>
                        <th>
                            Age
                        </th>
                    <th></th>
                </tr>
            </thead>
            <tbody>
            </tbody>
        </table>
        `;

        anchorElement.html(markup);

        this.configAuth.getFhirServerAccessInfo(function (fhirServerUrl, accessToken) {
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
                        var table = document.getElementById('patient-list-table');

                        $.each(result.entry, function (index, val) {
                            var row = table.insertRow(-1);
                            var familyName = row.insertCell(0);
                            familyName.innerText = val.resource.name[0].family;
                            var givenName = row.insertCell(1);
                            givenName.innerText = val.resource.name[0].given;
                            var age = row.insertCell(2);
                            age.innerText = getAgeFromDateString(val.resource.birthDate);
                            var links = row.insertCell(3);
                            links.innerHTML = '<a href="#" onClick="patientsModule.renderPatient(\'' + val.resource.id + '\');")>[Details]</a>';
                        });
                    }
                }
            );
        });
    }
}


function getAgeFromDateString(dateString) 
{
    var today = new Date();
    var birthDate = new Date(dateString);
    var age = today.getFullYear() - birthDate.getFullYear();
    var m = today.getMonth() - birthDate.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < birthDate.getDate())) 
    {
        age--;
    }
    return age;
}
