switchAdminContent = (element) => {
    $('ul.nav-tabs li.nav-item a.nav-link').each(function () {
        $(this).removeClass('active');
        if ($(this).attr("id") === $(element).attr("id")) {
            $(this).addClass('active');
        }
    });

    $('#navContainer').slideUp(function () {
        switch ($(element).attr('id')) {
            case 'generalSettings':
                loadGenericContent();
                break;
            case 'schedulerSettings':
                loadSchedulerContent();
                break;
            case 'statisticSettings':
                loadStatisticContent();
                break;
            case 'actionSettings':
                loadActionContent();
                break;
        }
    });
}

renderAdminModal = () => {
    let mainContent = '';
    mainContent +=  '<div class="modal fade bd-example-modal-xl" id="adminModal" tabIndex="-1" role="dialog" aria-labelledby="exampleModalLabel" aria-hidden="true">';
    mainContent +=  '    <div class="modal-dialog modal-xl" style="height: 80%;" role="document">';
    mainContent +=  '        <div class="modal-content" style="height: 80%;">';
    mainContent +=  '            <div class="modal-header">';
    mainContent +=  '                <h5 class="modal-title" id="optionModalLabel">Administration </h5>';
    mainContent +=  '                <button type="button" class="close" data-dismiss="modal" aria-label="Close">';
    mainContent +=  '                    <span aria-hidden="true">&times;</span>';
    mainContent +=  '                </button>';
    mainContent +=  '            </div>';
    mainContent +=  '            <div class="modal-body" style="max-height: calc(100% - 120px); overflow-y: scroll;">';
    mainContent +=  '                <ul class="nav nav-tabs nav-fill">';
    mainContent +=  '                    <li class="nav-item">';
    mainContent +=  '                       <a class="nav-link active" href="javascript:void(0);" onClick="switchAdminContent(this)" id="generalSettings">Allgemein</a>';
    mainContent +=  '                    </li>';
    mainContent +=  '                    <li class="nav-item">';
    mainContent +=  '                        <a class="nav-link" href="javascript:void(0);" onClick="switchAdminContent(this)" id="schedulerSettings">Scheduler</a>';
    mainContent +=  '                    </li>';
    mainContent +=  '                    <li class="nav-item">';
    mainContent +=  '                        <a class="nav-link" href="javascript:void(0);" onClick="switchAdminContent(this)" id="statisticSettings">Statistiken</a>';
    mainContent +=  '                    </li>';
    mainContent +=  '                    <li class="nav-item">';
    mainContent +=  '                        <a class="nav-link" href="javascript:void(0);" onClick="switchAdminContent(this)" id="actionSettings">Aktionen</a>';
    mainContent +=  '                    </li>';
    mainContent +=  '                </ul>';
    mainContent +=  '                <div class="form-row" style="display: none;" id="navContainer">&nbsp;</div>';
    mainContent +=  '            </div>';
    mainContent +=  '            <div class="modal-footer">';
    mainContent +=  '                <button type="button" class="btn btn-secondary" data-dismiss="modal">Schließen</button>';
    mainContent +=  '            </div>';
    mainContent +=  '        </div>';
    mainContent +=  '    </div>';
    mainContent +=  '</div>';    
    return mainContent;
}

loadGenericContent = () => {
    $.ajax({
        url: "/api/administration/getGenericContentData",
        dataType: "json",
        method: "GET",
    })
        .done(function (result) {
            $('#navContainer')
                .html(renderAdminGenericModal(result))
                .slideDown();
        })
        .fail(function () {
            showAlert("Ein Fehler ist beim abrufen von Daten aufgetreten!", "alert-danger");
        });
}

loadSchedulerContent = () => {
    $.ajax({
        url: "/api/administration/getSchedulerContentData",
        dataType: "json",
        method: "GET",
    })
        .done(function (result) {
            $('#navContainer')
                .html(renderAdminSchedulerModal(result))
                .slideDown();
        })
        .fail(function () {
            showAlert("Ein Fehler ist beim abrufen von Daten aufgetreten!", "alert-danger");
        });
}

loadStatisticContent = () => {
    $.ajax({
        url: "/api/administration/getStatisticsContent",
        dataType: "html",
        method: "GET",
    })
        .done(function (result) {
            $('#navContainer')
                .html(result)
                .slideDown();
        })
        .fail(function () {
            showAlert("Ein Fehler ist beim abrufen von Daten aufgetreten!", "alert-danger");
        });
}

loadActionContent = () => {
    $.ajax({
        url: "/api/administration/getActionContentData",
        dataType: "json",
        method: "GET",
    })
        .done(function (result) {
            $('#navContainer')
                .html(renderAdminActionModal(result))
                .slideDown();
        })
        .fail(function () {
            showAlert("Ein Fehler ist beim abrufen von Daten aufgetreten!", "alert-danger");
        });
}