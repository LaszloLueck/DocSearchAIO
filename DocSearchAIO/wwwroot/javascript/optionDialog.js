let openOptionDialog = () => {
    let optionData = {
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
        url: "/api/administration/getOptionsDialogData",
        data: JSON.stringify(optionData)
    })
        .done(function (result) {
            $("#modalContainer").append(renderResultPageConfigModal(result));
            const element = $('#optionModal');
            $(element).on('hidden.bs.modal', function (e) {
                $(element).remove();
            });
            $(element).modal('show');
        })
        .fail(function () {
            showAlert("Ein Fehler ist beim abrufen von Daten aufgetreten!", "alert-danger");
        });

}

let checkCheckboxElementsFilter = (element) => {
    const grpName = $(element).attr('name');
    const elements = $('input[name=' + grpName + ']');
    let selected = 0;
    $(elements).each(function (inner, innerElement) {
        if ($(innerElement).prop('checked'))
            selected += 1
    });
    if (selected === 0) {
        $(element).prop('checked', true)
        showAlert("Es muss mindestens ein Suchindex ausgewählt sein!", "alert-danger");
    }
    localStorage.setItem("filterWord", $('#filterWord').prop('checked'));
    localStorage.setItem("filterExcel", $('#filterExcel').prop('checked'));
    localStorage.setItem("filterPowerpoint", $('#filterPowerpoint').prop('checked'));
    localStorage.setItem("filterPdf", $('#filterPdf').prop('checked'));

}

let switchSizeDropDown = (value) => {
    let selector = $('#listSizeSelector a');
    $(selector).removeClass('active');
    $('#switchSize_' + value).addClass('active');
    const selField = $(selector).filter('.active').first().text();
    $('#listSizeDropdown').html(selField);
    localStorage.setItem("itemsPerPage", selField);
}