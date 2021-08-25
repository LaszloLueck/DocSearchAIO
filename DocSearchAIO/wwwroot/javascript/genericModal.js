let addNewElasticNode = () => {
    let currentElements = $('#elasticNodeContainer > div').length;

    let element = '<div style="display: none" class="input-group input-group-sm m-2" id="elasticEndpoint_' + (currentElements + 1) + '">';
    element += '<div class="input-group-prepend">';
    element += '<span class="input-group-text" id="labelElasticEndpoint_' + (currentElements + 1) + '">Elastic-Endpunkt ' + (currentElements + 1) + '</span>'
    element += '</div>';
    element += '<input type="text" class="form-control mr-1" id="elastic_endpoint_' + (currentElements + 1) + '" placeholder="Elastice-Node als http(s)://Host:Port" value="">';
    element += '<button type="button" class="btn btn-sm btn-danger" onclick="removeNodeEntry(' + (currentElements + 1) + ')"><i class="bi bi-trash"></i> Entfernen</button>';
    element += '</div>';

    $('#elasticNodeContainer').append(element);
    $('#elasticEndpoint_' + (currentElements + 1)).fadeIn();
}

let removeNodeEntry = (id) => {
    $('#elasticEndpoint_' + id).fadeOut(function () {
        $("#elasticEndpoint_" + id).remove();
    });
}

let saveAdminContent = () => {
    let elasticNodes = $('input[id^="elastic_endpoint_"]').map(function () {
        return this.value;
    })
        .get();

    let processors = $('div[id^="processorType_"]').map(function () {
        let getter = $(this).attr('id').replace('processorType_', '');
        return {
            processorType: getter,
            runsEvery: parseInt($('#' + getter + '_runsEvery').val()) || 3600,
            startDelay: parseInt($('#' + getter + '_startDelay').val()) || 10,
            parallelism: parseInt($('#' + getter + '_parallelism').val()) || 1,
            fileExtension: $('#' + getter + '_fileExtension').val(),
            excludeFilter: $('#' + getter + '_fileFilter').val(),
            jobName: $('#' + getter + '_schedulerJobName').val(),
            triggerName: $('#' + getter + '_schedulerTriggerName').val(),
            indexSuffix: $('#' + getter + '_indexSuffix').val()
        };
    })
        .get();

    let cleanups = $('div[id^="cleanupType_"]').map(function () {
        let getter = $(this).attr('id').replace('cleanupType_', '');
        return {
            cleanupType: getter,
            runsEvery: parseInt($('#' + getter + '_runsEvery').val()) || 3600,
            startDelay: parseInt($('#' + getter + '_startDelay').val()) || 10,
            parallelism: parseInt($('#' + getter + '_parallelism').val()) || 1,
            jobName: $('#' + getter + '_cleanupJobName').val(),
            triggerName: $('#' + getter + '_cleanupTriggerName').val(),
            forComparer: $('#' + getter + '_cleanupForComparerName').val(),
            forIndexSuffix: $('#' + getter + '_cleanupForIndexSuffix').val()
        }
    })
        .get();

    let dictionary = Object.assign({}, ...processors.map((element) => {
        let newKey = element.processorType;
        delete (element.processorType);
        return ({[newKey]: element});
    }));

    let cleanupDictionary = Object.assign({}, ...cleanups.map((element) => {
        let newKey = element.cleanupType;
        delete (element.cleanupType);
        return ({[newKey]: element});
    }));

    let data = {
        scanPath: $('#scanPath').val(),
        indexName: $('#indexName').val(),
        schedulerName: $('#schedulerName').val(),
        actorSystemName: $('#actorSystemName').val(),
        processorGroupName: $('#groupName').val(),
        cleanupGroupName: $('#cleanupGroupName').val(),
        schedulerId: $('#schedulerId').val(),
        elasticEndpoints: elasticNodes,
        uriReplacement: $('#urlReplacement').val(),
        comparerDirectory: $('#comparerDirectory').val(),
        statisticsDirectory: $('#statisticsDirectory').val(),
        processorConfigurations: dictionary,
        cleanupConfigurations: cleanupDictionary
    };

    console.log(JSON.stringify(data));

    $.ajax({
        url: "/api/administration/setGenericContent",
        dataType: "json",
        contentType: "application/json",
        method: "POST",
        data: JSON.stringify(data)
    }).done(function (result) {
        if (result) {
            showAlert("Daten wurden erfolgreich gespeichert!", "alert-info");
        } else {
            showAlert("Die Daten konnten nicht gespeichert werden!", "alert-danger");
        }

        $('#adminModal').modal('toggle');
    }).fail(function () {
        showAlert("Ein Fehler ist beim speichern der Daten aufgetreten!", "alert-danger");
    });
};