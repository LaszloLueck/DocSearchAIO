let init = () => {

    let data = {
        filterExcel: localStorage.getItem("filterExcel") === 'true',
        filterWord: localStorage.getItem("filterWord") === 'true',
        filterPowerpoint: localStorage.getItem("filterPowerpoint") === 'true',
        filterPdf: localStorage.getItem("filterPdf") === 'true',
        filterMsg: localStorage.getItem("filterMsg") === 'true',
        filterEml: localStorage.getItem("filterEml") === 'true',
        itemsPerPage: localStorage.getItem("itemsPerPage") ?? undefined
    };

    $.ajax({
        url: "/api/base/init",
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
            localStorage.setItem("filterMsg", result.filterMsg);
            localStorage.setItem("filterEml", result.filterEml);
            localStorage.setItem("wordFilterActive", result["wordFilterActive"]);
            localStorage.setItem("excelFilterActive", result["excelFilterActive"]);
            localStorage.setItem("powerpointFilterActive", result["powerpointFilterActive"]);
            localStorage.setItem("msgFilterActive", result["msgFilterActive"]);
            localStorage.setItem("emlFilterActive", result["emlFilterActive"]);
            localStorage.setItem("pdfFilterActive", result["pdfFilterActive"]);
        })
        .fail(function () {
            showAlert("Ein Fehler ist beim abrufen von Daten aufgetreten!", "alert-danger");
        });

};

let checkIfAnyIndexActive = () => {
    return localStorage.getItem("filterWord") === 'true' ||
        localStorage.getItem("filterExcel") === 'true' ||
        localStorage.getItem("filterPowerpoint") === 'true' ||
        localStorage.getItem("filterPdf") === 'true' ||
        localStorage.getItem("filterMsg") === 'true' ||
        localStorage.getItem("filterEml") === 'true';
}

let setAutoCompleteWithCondition = () => {
    if (checkIfAnyIndexActive()) {
        $('#searchField').autoComplete({
            resolver: 'custom',
            noResultsText: 'Keine Ergebnisse',
            events: {
                search: function (qry, callback) {
                    let lastElement = $("#searchField").val().split(" ");
                    let currentStep = lastElement[lastElement.length - 1];
                    $.ajax({
                        url: "/api/search/doSuggest",
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
                        .fail(function () {
                            showAlert("Ein Fehler ist beim abrufen von Daten aufgetreten!", "alert-danger");
                        });
                },
                searchPost: function (rsf) {
                    return rsf.map(function (k) {
                        return k.label
                    });
                }
            }
        })
    }
}

let escapeMarkup = (unsafe) => {
    let prep = unsafe.replaceAll('<span style="color:orange;">', "##COLORGRADESTART##").replaceAll('</span>',"##COLORGRADEEND##");
    return prep.replace(/[<>&'"]/g, function (c) {
        switch (c) {
            case '<':
                return '&lt;';
            case '>':
                return '&gt;';
            case '&':
                return '&amp;';
            case '\'':
                return '&apos;';
            case '"':
                return '&quot;';
            case '':
                return c;
            case '</span>':
                return c;
        }
    }).replaceAll('##COLORGRADESTART##','<span style="color:orange;"><strong>').replaceAll('##COLORGRADEEND##', '</strong></span>');
}

let getAdministrationModal = () => {
    $('#modalContainer').append(renderAdminModal());
    const element = $('#adminModal');
    $(element).on('hidden.bs.modal', function () {
        $(element).remove();
    });
    $(element).modal('show');
    switchAdminContent($('#generalSettings'));
}

let showAlert = (alertText, alertType) => {
    $('body').append('<div style="display: none; position: fixed; top: 0; left: 0; width: 100%;" class="alert ' + alertType + '" role="alert" id="customAlert">' + alertText +
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

$(function () {
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

    $('#openAdministration').click(function (event) {
        getAdministrationModal();
    })

})


