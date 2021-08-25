let renderResultPageConfigModal = (result) => {
    let mainContent = '';

    mainContent += '<div class="modal fade bd-example-modal-xl" id="optionModal" tabIndex="-1" role="dialog" aria-labelledby="exampleModalLabel" aria-hidden="true">';
    mainContent += '     <div class="modal-dialog modal-xl" role="document">';
    mainContent += '         <div class="modal-content">';
    mainContent += '             <div class="modal-header">';
    mainContent += '                 <h5 class="modal-title" id="optionModalLabel">Suchoptionen </h5>';
    mainContent += '                 <button type="button" class="close" data-dismiss="modal" aria-label="Close">';
    mainContent += '                     <span aria-hidden="true">&times;</span>';
    mainContent += '                 </button>';
    mainContent += '             </div>';
    mainContent += '             <div class="modal-body">';
    mainContent += '                 <div class="form-row">';
    mainContent += '                     <div class="input-group mb-2 col-md-6 ">';
    mainContent += '                         <div class="input-group-prepend">';
    mainContent += '                        <span class="input-group-text" id="labelCountResultsPerPage">Anzahl Suchergebnisse pro Seite</span>';
    mainContent += '                         </div>';
    mainContent += '                         <div class="dropdown">';
    mainContent += '                            <button class="btn btn-secondary dropdown-toggle" type="button" id="listSizeDropdown" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">';
    mainContent += result.itemsPerPage;
    mainContent += '                             </button>';
    mainContent += '                             <div class="dropdown-menu" aria-labelledby="listSizeDropdown" id="listSizeSelector">';

    let active20 = result.itemsPerPage === 20 ? "active" : "";
    let active50 = result.itemsPerPage === 50 ? "active" : "";
    let active75 = result.itemsPerPage === 75 ? "active" : "";
    let active100 = result.itemsPerPage === 100 ? "active" : "";

    mainContent += '                                <a class="dropdown-item ' + active20 + '" href="javascript:switchSizeDropDown(20)" id="switchSize_20">20</a>';
    mainContent += '                                <a class="dropdown-item ' + active50 + '" href="javascript:switchSizeDropDown(50)" id="switchSize_50">50</a>';
    mainContent += '                                <a class="dropdown-item ' + active75 + '" href="javascript:switchSizeDropDown(75)" id="switchSize_75">75</a>';
    mainContent += '                                <a class="dropdown-item ' + active100 + '" href="javascript:switchSizeDropDown(100)" id="switchSize_100">100</a>';
    mainContent += '                             </div>';
    mainContent += '                         </div>';
    mainContent += '                     </div>';
    mainContent += '                 </div>';
    mainContent += '                 <div class="form-row">';
    mainContent += '                     <div class="input-group mb-2 col-md-12 ">';
    mainContent += '                         <div class="input-group-prepend">';
    mainContent += '                             <span class="input-group-text" id="labelSelectDocumentTypes">Dokument-Filter</span>';
    mainContent += '                         </div>';
    let filterWord = result.filterWord ? "checked" : "";
    let filterExcel = result.filterExcel ? "checked" : "";
    let filterPowerpoint = result.filterPowerpoint ? "checked" : "";
    let filterPdf = result.filterPdf ? "checked" : "";

    let wordIndexExists = result.wordIndexExists ? "" : "disabled";
    let excelIndexExists = result.excelIndexExists ? "" : "disabled";
    let powerpointIndexExists = result.powerpointIndexExists ? "" : "disabled";
    let pdfIndexExists = result.pdfIndexExists ? "" : "disabled";

    mainContent += '                         <ul class="list-group list-group-horizontal">';
    mainContent += '                            <li class="list-group-item"><input id="filterWord" name="docFilter" ' + wordIndexExists + ' ' + filterWord + ' onchange="checkCheckboxElementsFilter(this)" type="checkbox" aria-label="Checkbox for following text input" class="mr-3 ml-3"><img src="./images/word.svg" width="40" height="40" alt="Wordfilter"/></li>';
    mainContent += '                            <li class="list-group-item"><input id="filterExcel" name="docFilter" ' + excelIndexExists + ' ' + filterExcel + ' onchange="checkCheckboxElementsFilter(this)" type="checkbox" aria-label="Checkbox for following text input" class="mr-3 ml-3"><img src="./images/excel.svg" width="40" height="40" alt="Excelfilter"/></li>';
    mainContent += '                            <li class="list-group-item"><input id="filterPowerpoint" name="docFilter" ' + powerpointIndexExists + ' ' + filterPowerpoint + ' onchange="checkCheckboxElementsFilter(this)" type="checkbox" aria-label="Checkbox for following text input" class="mr-3 ml-3"><img src="./images/powerpoint.svg" width="40" height="40" alt="Powerpointfilter"/></li>';
    mainContent += '                            <li class="list-group-item"><input id="filterPdf" name="docFilter" ' + pdfIndexExists + ' ' + filterPdf + ' onchange="checkCheckboxElementsFilter(this)" type="checkbox" aria-label="Checkbox for following text input" class="mr-3 ml-3"><img src="./images/pdf.svg" width="40" height="40" alt="PDF-filter"/></li>';
    mainContent += '                         </ul>';
    mainContent += '                     </div>';
    mainContent += '                 </div>';
    mainContent += '             </div>';
    mainContent += '             <div class="modal-footer">';
    mainContent += '                 <button type="button" class="btn btn-secondary" data-dismiss="modal">Schließen</button>';
    mainContent += '             </div>';
    mainContent += '         </div>';
    mainContent += '     </div>';
    mainContent += ' </div>';

    return mainContent;
}