let openDocumentDetailDialog = (id) => {
    let data = {id: id};
    $.ajax({
        method: "POST",
        dataType: "json",
        contentType: "application/json",
        url: "/api/search/documentDetailData",
        data: JSON.stringify(data)
    })
        .done(function (result) {
            $("#modalContainer").html(getDocumentDetailData(result));
            $('#documentDetailModal')
                .on('hidden.bs.modal', function () {
                    $('#documentDetailModal').remove();
                }).modal('show');
        })
        .fail(function () {
            showAlert("Ein Fehler ist beim abrufen von Daten aufgetreten!", "alert-danger");
        });
}

let getDocumentDetailData = (result) => {
    return `<div class="modal fade bd-example-modal-xl" id="documentDetailModal" tabIndex="-1" role="dialog"
         aria-labelledby="exampleModalLabel" aria-hidden="true">
        <div class="modal-dialog modal-xl" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="exampleModalLabel">Dokumenteigenschaften </h5>
                    <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                        <span aria-hidden="true">&times;</span>
                    </button>
                </div>
                <div class="modal-body">
                    <div class="form-row">
                        <div class="input-group col-md-6 mb-2">
                            <div class="input-group-prepend">
                                <span class="input-group-text" id="labelState">Status</span>
                            </div>
                            <input type="text" disabled class="form-control" id="infoResponseState"
                                   value="OK">
                        </div>
                        <div class="input-group col-md-6 mb-2">
                            <div class="input-group-prepend">
                                <span class="input-group-text" id="labelId">Id</span>
                            </div>
                            <input type="text" disabled class="form-control" id="infoResponseId"
                                   value=` + result.id + `>
                        </div>
                    </div>
                    <div class="form-row">
                        <div class="input-group mb-2 col-md-6 ">
                            <div class="input-group-prepend">
                                <span class="input-group-text" id="labelCreator">Ersteller</span>
                            </div>
                            <input type="text" disabled class="form-control" id="infoFieldCreator"
                                   value="` + result.creator + `">
                        </div>
                        <div class="input-group mb-2 col-md-6 ">
                            <div class="input-group-prepend">
                                <span class="input-group-text" id="labelCreated">Am</span>
                            </div>
                            <input type="text" disabled class="form-control" id="infoFieldCreated"
                                   value="` + result.created + `">
                        </div>
                    </div>
                    <div class="form-row">
                        <div class="input-group mb-2 col-md-6 ">
                            <div class="input-group-prepend">
                                <span class="input-group-text" id="labelModifiedBy">Zuletzt geändert von</span>
                            </div>
                            <input type="text" disabled class="form-control" id="infoFieldModifiedBy"
                                   value="`+ result.lastModifiedBy +`">
                        </div>
                        <div class="input-group mb-2 col-md-6 ">
                            <div class="input-group-prepend">
                                <span class="input-group-text" id="labelModified">Am</span>
                            </div>
                            <input type="text" disabled class="form-control" id="infoFieldModified"
                                   value="` + result.lastModified + `">
                        </div>
                    </div>
                    <div class="form-row">
                        <div class="input-group mb-2 col-md-6 ">
                            <div class="input-group-prepend">
                                <span class="input-group-text" id="labelTitle">Titel</span>
                            </div>
                            <input type="text" disabled class="form-control" id="infoFieldTitle"
                                   value="` + result.title + `">
                        </div>
                        <div class="input-group mb-2 col-md-6 ">
                            <div class="input-group-prepend">
                                <span class="input-group-text" id="labelSubject">Betreff</span>
                            </div>
                            <input type="text" disabled class="form-control" id="infoFieldSubject"
                                   value="` + result.subject + `">
                        </div>
                    </div>
                    <div class="form-row">
                        <div class="input-group mb-2 col-md-6 ">
                            <div class="input-group-prepend">
                                <span class="input-group-text" id="labelVersion">Version</span>
                            </div>
                            <input type="text" disabled class="form-control" id="infoFieldVersion"
                                   value="` + result.version + `">
                        </div>
                        <div class="input-group mb-2 col-md-6 ">
                            <div class="input-group-prepend">
                                <span class="input-group-text" id="labelRevision">Revision</span>
                            </div>
                            <input type="text" disabled class="form-control" id="infoFieldRevision"
                                   value="` + result.revision + `">
                        </div>
                    </div>
                    <div class="form-row">
                        <div class="input-group mb-2 col-md-12">
                            <div class="input-group-prepend">
                                <span class="input-group-text" id="labelLastPrinted">Letzter Druck</span>
                            </div>
                            <input type="text" disabled class="form-control" id="infoFieldLastPrinted"
                                   value="` + result.lastPrinted + `">
                        </div>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-dismiss="modal">Schließen</button>
                </div>
            </div>
        </div>
    </div>`;
}