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

loadGenericContent = () => {
    $.ajax({
        url: "/api/administration/getGenericContent",
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

loadSchedulerContent = () => {
    $.ajax({
        url: "/api/administration/getSchedulerContent",
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
        url: "/api/administration/getActionContent",
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