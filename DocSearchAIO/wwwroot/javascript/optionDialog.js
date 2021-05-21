openOptionDialog = () => {
    var data = {
        filterWord: localStorage.getItem("filterWord") === "true",
        filterExcel: localStorage.getItem("filterExcel") === "true",
        filterPowerpoint: localStorage.getItem("filterPowerpoint") === "true",
        filterPdf: localStorage.getItem("filterPdf") === "true",
        itemsPerPage: localStorage.getItem("itemsPerPage")
    };

    $.ajax({
        method: "POST",
        dataType: "json",
        contentType: "application/json",
        url: "/api/administration/getOptionsDialog",
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