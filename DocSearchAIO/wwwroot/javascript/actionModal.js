instantStartJob = (jobId, groupId) => {
    let request = {
        jobName: jobId,
        groupId: groupId
    };
    $.ajax({
        url: "/api/administration/instantStartJob",
        method: "POST",
        contentType: "application/json",
        dataType: "json",
        data: JSON.stringify(request)
    })
        .done(function () {
            loadActionContent()
        })
        .fail(function () {
            showAlert("Ein Fehler ist beim abrufen von Daten aufgetreten!", "alert-danger");
        });
}

reindexAndStartJob = (jobId, groupId) => {
    let request = {
        jobName: jobId,
        groupId: groupId
    };
    $.ajax({
        url: "/api/administration/reindexAndStartJob",
        method: "POST",
        contentType: "application/json",
        dataType: "json",
        data: JSON.stringify(request)
    })
        .done(function () {
            loadActionContent()
        })
        .fail(function () {
            showAlert("Ein Fehler ist beim abrufen von Daten aufgetreten!", "alert-danger");
        });
}

resumeTrigger = (triggerId, triggerGroup) => {
    let request = {
        triggerId: triggerId,
        groupId: triggerGroup
    };

    $.ajax({
        url: "/api/administration/resumeTrigger",
        method: "POST",
        contentType: "application/json",
        dataType: "json",
        data: JSON.stringify(request)
    })
        .done(function () {
            loadActionContent()
        })
        .fail(function () {
            showAlert("Ein Fehler ist beim abrufen von Daten aufgetreten!", "alert-danger");
        });
}

pauseTrigger = (triggerId, triggerGroup) => {
    let request = {
        triggerId: triggerId,
        groupId: triggerGroup
    };

    $.ajax({
        url: "/api/administration/pauseTrigger",
        method: "POST",
        contentType: "application/json",
        dataType: "json",
        data: JSON.stringify(request)
    })
        .done(function () {
            loadActionContent()
        })
        .fail(function () {
            showAlert("Ein Fehler ist beim abrufen von Daten aufgetreten!", "alert-danger");
        });
}