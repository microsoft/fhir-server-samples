class PatientModule
{
    constructor(anchor, configAuthModule)
    {
        this.anchor = anchor;
        this.configAuth = configAuthModule;
        this.patientList = [];
        this.maxPatientsPerPage = 10;
        this.currentPatientIndex = 0;
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
        var onset = row.insertCell(-1);
        onset.innerText = conditionResource.onsetDateTime;
        var description = row.insertCell(-1);
        description.innerText = conditionResource.code.text;
        var status = row.insertCell(-1);
        status.innerText = conditionResource.clinicalStatus;
        conditionCount.innerText = Number(conditionCount.innerText) + 1;
        var links = row.insertCell(-1);
        links.innerHTML = '<a class="btn" href="#" onClick="resourceModule.show(\'/Condition/' + conditionResource.id + '\');")><i class="fas fa-fire"></i></a>';
    }

    addPatientEncounter(resource)
    {
        var table = document.getElementById('patient-encounter-table');
        var count = document.getElementById('patient-encounter-count');
        var row = table.insertRow(-1);
        var date = row.insertCell(-1);
        date.innerText = resource.period.start;
        var title = row.insertCell(-1);
        title.innerText = resource.type[0].text;
        var id = row.insertCell(-1);
        id.innerText = resource.id;
        count.innerText = Number(count.innerText) + 1;
        var links = row.insertCell(-1);
        links.innerHTML = '<a class="btn" href="#" onClick="resourceModule.show(\'/Encounter/' + resource.id + '\');")><i class="fas fa-fire"></i></a>';

    }

    addPatientObservation(resource)
    {
        var table = document.getElementById('patient-observation-table');
        var count = document.getElementById('patient-observation-count');
        var row = table.insertRow(-1);
        var date = row.insertCell(-1);
        date.innerText = resource.issued;
        var title = row.insertCell(-1);
        title.innerText = resource.code.text;
        var id = row.insertCell(-1);
        id.innerText = resource.id;
        count.innerText = Number(count.innerText) + 1;
        var links = row.insertCell(-1);
        links.innerHTML = '<a class="btn" href="#" onClick="resourceModule.show(\'/Observation/' + resource.id + '\');")><i class="fas fa-fire"></i></a>';
    }

    getPatientSearchNextLink(callback)
    {
        var ptMod = this;
        if (ptMod.patientSearchNextLink === undefined) {
            ptMod.configAuth.getFhirServerUrl(function(fhirServerUrl) {
                ptMod.patientSearchNextLink = fhirServerUrl + '/Patient';
                callback(ptMod.patientSearchNextLink);
            });
        } else {
            callback(ptMod.patientSearchNextLink);
        }
    }

    fetchPatients(patientsNeeded, callback)
    {
        var ptMod = this;

        //Let's see if we have the patients already
        if (patientsNeeded < ptMod.patientList.length)
        {
            callback();
            return;
        }

        //Otherwise get some patients
        this.configAuth.getFhirServerAccessInfo(function (fhirServerUrl, accessToken) {
            ptMod.getPatientSearchNextLink(function(searchUrl){
                if (searchUrl) {
                    //Get all patients
                    $.ajax(
                        {
                            type: 'GET',
                            url: searchUrl,
                            beforeSend: function (xhdr) {
                                xhdr.setRequestHeader('Authorization', 'Bearer ' + accessToken);
                            },
                            success: function (result, status) {
                                ptMod.patientSearchNextLink = null; //Assume we are at the end
                                if (Array.isArray(result.link))
                                {
                                    for (var i = 0; i < result.link.length; i++) 
                                    {
                                        if (result.link[i].relation == "next") 
                                        {
                                            //Set new search link
                                            ptMod.patientSearchNextLink = result.link[i].url;
                                            break;
                                        }
                                    }
                                }
    
                                if (Array.isArray(result.entry))
                                {
                                    for (var i = 0; i < result.entry.length; i++) 
                                    {
                                        ptMod.patientList.push(result.entry[i].resource);
                                    }
                                }

                                if (ptMod.patientSearchNextLink && patientsNeeded > ptMod.patientList.length) 
                                {
                                    ptMod.fetchPatients(patientsNeeded, callback);
                                }
                                else
                                {
                                    callback();
                                }
                            }
                        }
                    );
                }
                else
                {
                    callback();
                }
            });
        });
    }

    forwardPatientList()
    {
        this.currentPatientIndex += this.maxPatientsPerPage;
        this.renderPatientList();
    }
    
    reversePatientList()
    {
        this.currentPatientIndex -= this.maxPatientsPerPage;
        if (this.currentPatientIndex < 0)
        {
            this.currentPatientIndex = 0;
        }
        this.renderPatientList();
    }

    renderPatientList()
    {
        var ptMod = this;

        this.fetchPatients(ptMod.currentPatientIndex + ptMod.maxPatientsPerPage, function() {
            var anchorElement = ptMod.anchor;

            var markup = `
            <div>
            <a href="#" id="patient-list-rev" class="btn disabled"><i class='fas fa-angle-left'></i></a>
            <a href="#" id="patient-list-fwd" class="btn disabled"><i class='fas fa-angle-right'></i></a>
            </div>
            <table id="patient-list-table" class="table">
                <thead>
                    <tr>
                            <th>
                            </th>
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

            var table = document.getElementById('patient-list-table');

            for (var i = ptMod.currentPatientIndex; 
                 (i < (ptMod.currentPatientIndex + ptMod.maxPatientsPerPage)) && (i < ptMod.patientList.length); 
                 i++)
            {
                var currentPt = ptMod.patientList[i];
                var row = table.insertRow(-1);
                var patientIndex = row.insertCell(-1);
                patientIndex.innerHTML = i+1;
                var familyName = row.insertCell(-1);
                familyName.innerText = currentPt.name[0].family;
                var givenName = row.insertCell(-1);
                givenName.innerText = currentPt.name[0].given;
                var age = row.insertCell(-1);
                age.innerText = getAgeFromDateString(currentPt.birthDate);
                var links = row.insertCell(-1);
                links.innerHTML = '<a class="btn" href="#" onClick="patientsModule.renderPatient(\'' + currentPt.id + '\');")><i class="fas fa-info-circle"></i></a>' +
                                  '<a class="btn" href="#" onClick="resourceModule.show(\'/Patient/' + currentPt.id + '\');")><i class="fas fa-fire"></i></a>';
            }

            if (ptMod.currentPatientIndex > 0)
            {
                var revLink = document.getElementById('patient-list-rev');
                revLink.className = "btn enabled";
                revLink.addEventListener('click', function() {
                    ptMod.reversePatientList()
                });
            }

            if (((ptMod.currentPatientIndex + ptMod.maxPatientsPerPage) < ptMod.patientList.length) ||
                (ptMod.patientSearchNextLink))
            {
                var fwdLink = document.getElementById('patient-list-fwd');
                fwdLink.className = "btn enabled";
                fwdLink.addEventListener('click', function() {
                    ptMod.forwardPatientList()
                });
            }

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
