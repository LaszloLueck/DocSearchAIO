init = () => {
    const data = {
        filterExcel: localStorage.getItem("filterExcel") === 'true',
        filterWord: localStorage.getItem("filterWord") === 'true',
        filterPowerpoint: localStorage.getItem("filterPowerpoint") === 'true',
        filterPdf: localStorage.getItem("filterPdf") === 'true',
        itemsPerPage: localStorage.getItem("itemsPerPage") ?? undefined
    };

    $.ajax({
        url: "https://localhost:5001/Init",
        dataType: "json",
        method: "POST",
        contentType: "application/json",
        data: JSON.stringify(data)
    })
        .done(function (result) {
            localStorage.setItem("itemsPerPage", result.itemsPerPage);
            localStorage.setItem("filterWord", result.filterWord);
            localStorage.setItem("filterExcel", result.filterExcel);
            localStorage.setItem("filterPowerpoint", result.filterPowerpoint);
            localStorage.setItem("filterPdf", result.filterPdf);
            localStorage.setItem("wordFilterActive", result.wordFilterActive);
            localStorage.setItem("excelFilterActive", result.excelFilterActive);
            localStorage.setItem("powerpointFilterActive", result.powerpointFilterActive);
            localStorage.setItem("pdfFilterActive", result.pdfFilterActive);
        })
        .fail(function (xhr, status, error) {
            showAlert("Ein Fehler ist beim abrufen von Daten aufgetreten!", "alert-danger");
        });

};

checkIfAnyIndexActive = () => {
    return localStorage.getItem("filterWord") === 'true' ||
        localStorage.getItem("filterExcel") === 'true' ||
        localStorage.getItem("filterPowerpoint") === 'true' ||
        localStorage.getItem("filterPdf") === 'true';
}

setAutoCompleteWithCondition = () => {
    if (checkIfAnyIndexActive()) {
        $('#searchField').autoComplete({
            resolver: 'custom',
            noResultsText: 'Keine Ergebnisse',
            events: {
                search: function (qry, callback) {
                    const lastElement = $("#searchField").val().split(" ");
                    const currentStep = lastElement[lastElement.length - 1];
                    $.ajax({
                        url: "https://localhost:5001/SearchSuggest",
                        dataType: "json",
                        method: "POST",
                        contentType: "application/json",
                        data: JSON.stringify({
                            searchPhrase: currentStep
                        })
                    })
                        .done(function (result) {
                            callback(result.suggests);
                        })
                        .fail(function (xhr, status, error) {
                            showAlert("Ein Fehler ist beim abrufen von Daten aufgetreten!", "alert-danger");
                        });
                },
                searchPost: function (rsf, orig) {
                    return rsf.map(function (k) {
                        return k.label
                    });
                }
            }
        })
    }
}

showAlert = (alertText, alertType) => {
    $('body').append('<div style="display: none; position: fixed; top: 0px; left: 0px; width: 100%;" class="alert ' + alertType + '" role="alert" id="customAlert">' + alertText + 
        '<button type="button" class="close" data-dismiss="alert" aria-label="Close">\n' +
        '<span aria-hidden="true">&times;</span>\n' +
        '</button>' +
        '</div>');
    $("#customAlert").fadeTo(2000, 500).fadeIn(500, function () {
        $("#customAlert").fadeOut(500, function () {
            $('#customAlert').alert('close');
        });
    });
}

$(document).ready(function () {
    init();
    setAutoCompleteWithCondition();

    $('#searchField').keyup(function (e) {
        const code = e.key;
        if (code === "Enter") {
            if (checkIfAnyIndexActive()) {
                doSearch($('#searchField').val(), 0);
            } else {
                showAlert('Kein Suchindex in den Optionen ausgewählt oder kein Suchindex vorhanden.', 'alert-warning');
            }
        }

        e.preventDefault();
    });

    $('#submitSearch').click(function (event) {
        if (checkIfAnyIndexActive()) {
            doSearch($('#searchField').val(), 0);
        } else {
            showAlert('Kein Suchindex in den Optionen ausgewählt oder kein Suchindex vorhanden.', 'alert-warning');
        }
        event.preventDefault();
    });

})


