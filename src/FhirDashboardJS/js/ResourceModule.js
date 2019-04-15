class ResourceModule
{
    constructor(resourceModal, configAuth)
    {
        this.resourceModal = resourceModal;
        this.configAuth = configAuth;
    }

    show(resourceUrl)
    {
        var resMod = this;

        this.configAuth.getFhirServerAccessInfo(function (fhirServerUrl, accessToken) {
            var fullQuery = fhirServerUrl + resourceUrl;
            $.ajax(
                {
                    type: 'GET',
                    url: fullQuery,

                    beforeSend: function (xhdr) {
                        xhdr.setRequestHeader('Authorization', 'Bearer ' + accessToken);
                    },
                    success: function(data, status) {
                        var markup = `
                            <div class="modal-dialog modal-lg">
                                <div class="modal-content">
                                    <div class="modal-header">
                                        <button type="button" class="close" data-dismiss="modal">&times;</button>
                                        <h4 id="resource-modal-title" class="modal-title">${resourceUrl}</h4>
                                    </div>
                                    <div id="resource-model-body" class="modal-body">
                                        <pre>${JSON.stringify(data, null, 2)}</pre>
                                    </div>
                                    <div class="modal-footer">
                                        <button type="button" class="btn btn-default" data-dismiss="modal">Close</button>
                                    </div>
                                </div>
                            </div>
                        `;
            
                        resMod.resourceModal.html(markup);
                        resMod.resourceModal.modal('show');
                    }
                }
            );
        });

    }
}