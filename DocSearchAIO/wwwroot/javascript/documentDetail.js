openDocumentDetailDialog = (id) => {
    let data = {id: id};
    $.ajax({
        method: "POST",
        dataType: "html",
        contentType: "application/json",
        url: "/api/search/documentDetail",
        data: JSON.stringify(data)
    })
        .done(function (result) {
            $("#modalContainer").html(result);
            $('#documentDetailModal')
                .on('hidden.bs.modal', function () {
                    $('#documentDetailModal').remove();
                }).modal('show');
        })
        .fail(function () {
            showAlert("Ein Fehler ist beim abrufen von Daten aufgetreten!", "alert-danger");
        });
}