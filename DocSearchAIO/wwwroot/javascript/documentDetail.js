openDocumentDetailDialog = (id) => {
    var data = {id: id};
    $.ajax({
        method: "POST",
        dataType: "json",
        contentType: "application/json",
        url: "https://localhost:5001/api/search/documentDetail",
        data: JSON.stringify(data)
    })
        .done(function (result) {
            $("body").append(result.content);
            $(result.elementName).on('hidden.bs.modal', function (e) {
                $(result.elementName).remove();
            });
            $(result.elementName).modal('show');
        })
        .fail(function (xhr, status, error) {
            showAlert("Ein Fehler ist beim abrufen von Daten aufgetreten!", "alert-danger");
        });
}