openDocumentDetailDialog = (id) => {
    let data = {id: id};
    $.ajax({
        method: "POST",
        dataType: "json",
        contentType: "application/json",
        url: "/api/search/documentDetail",
        data: JSON.stringify(data)
    })
        .done(function (result) {
            $("body").append(result.content);
            let element = $(result.elementName);
            $(element).on('hidden.bs.modal', function () {
                $(element).remove();
            });
            $(element).modal('show');
        })
        .fail(function () {
            showAlert("Ein Fehler ist beim abrufen von Daten aufgetreten!", "alert-danger");
        });
}