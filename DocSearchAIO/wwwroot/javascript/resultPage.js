checkCheckboxElementsFilter = (element) => {
    const grpName = $(element).attr('name');
    const elements = $('input[name=' + grpName + ']');
    let selected = 0;
    $(elements).each(function (inner, element) {
        if ($(element).prop('checked'))
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

switchSizeDropDown = (value) => {
    let selector = $('#listSizeSelector a');
    $(selector).removeClass('active');
    $('#switchSize_' + value).addClass('active');
    const selField = $(selector).filter('.active').first().text();
    $('#listSizeDropdown').html(selField);
    localStorage.setItem("itemsPerPage", selField)
}