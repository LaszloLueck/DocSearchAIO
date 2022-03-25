let calculateTriggerCurrentState = (triggerState) => {
    let triggerBatch = '<span class="badge badge-secondary">Unbekannt</span>';

    switch (triggerState) {
        case "blocked":
            triggerBatch = '<span class="badge badge-warning">Blockiert</span>';
            break;
        case "complete":
            triggerBatch = '<span class="badge badge-secondary">Komplett</span>';
            break;
        case "error":
            triggerBatch = '<span class="badge badge-danger">Fehler</span>';
            break;
        case "none":
            triggerBatch = '<span class="badge badge-info">Kein Status</span>';
            break;
        case "normal":
            triggerBatch = '<span class="badge badge-success">Normal</span>';
            break;
        case "paused":
            triggerBatch = '<span class="badge badge-primary">Pausiert</span>';
            break;
    }
    
    return triggerBatch;
};

let calculateTrigger = (key, triggerModel) => {
        let currentState = triggerModel.currentState.toLowerCase();
    let mainContent = '';
    
    mainContent += '<div class="container mb-3 border rounded border-color-gray">';
    mainContent += '    <div class="row mb-2">';
    mainContent += '        <div class="col-3">Name</div>';
    mainContent += '        <div class="col-9">' + triggerModel.triggerName + '</div>';
    mainContent += '    </div>';
    mainContent += '    <div class="row mb-2">';
    mainContent += '        <div class="col-3">Aktueller Status</div>';
    mainContent += '        <div class="col-9">' + calculateTriggerCurrentState(currentState) + '</div>';
    mainContent += '    </div>';
    mainContent += '    <div class="row mb-2">';
    mainContent += '        <div class="col-3">Aktion</div>';
    mainContent += '        <div class="col-9">';
    if (currentState === "paused") {
        mainContent += '                <button type="button" disabled class="btn btn-secondary mr-2"><i class="bi bi-pause-btn"></i> Pause</button>';
        if (currentState !== "blocked") {
            mainContent += '                <button type="button" class="btn btn-success mr-2" onclick="resumeTrigger(\'' + triggerModel.triggerName + '\',\'' + triggerModel.groupName + '\')"><i class="bi bi-play-btn"></i> Start</button>';
        }
    } else {
        mainContent += '               <button type="button" class="btn btn-warning mr-2" onClick="pauseTrigger(\'' + triggerModel.triggerName + '\',\'' + triggerModel.groupName + '\')"><i class="bi bi-pause-btn"></i> Pause</button>';
        mainContent += '               <button type="button" disabled class="btn btn-secondary mr-2"><i class="bi bi-play-btn"></i> Start</button>';
    }
    if (key === "docSearch_Processing") {
        if (triggerModel.jobState === 0 || currentState === "paused") {
            mainContent += '                <button type="button" class="btn btn-secondary mr-2" disabled><i class="bi bi-recycle"></i> Indexieren</button>';
            mainContent += '                <button type="button" class="btn btn-secondary mr-2" disabled><i class="bi bi-exclamation-diamond"></i> Reindexieren</button>';
        } else {
            mainContent += '                <button type="button" class="btn btn-orange mr-2" onclick="instantStartJob(\'' + triggerModel.jobName + '\',\'' + triggerModel.groupName + '\')"><i class="bi bi-recycle"></i> Indexieren</button>';
            mainContent += '                <button type="button" class="btn btn-danger mr-2" onclick="reindexAndStartJob(\'' + triggerModel.jobName + '\',\'' + triggerModel.groupName + '\')"><i class="bi bi-exclamation-diamond"></i> Reindexieren</button>';
        }
    } else {
        if (triggerModel.jobState === 0 || currentState === "paused") {
            mainContent += '                <button type="button" class="btn btn-secondary mr-2" disabled><i class="bi bi-recycle"></i> Cleanup</button>';
        } else {
            mainContent += '                <button type="button" class="btn btn-orange mr-2" onclick="instantStartJob(\'' + triggerModel.jobName + '\',\'' + triggerModel.groupName + '\')"><i class="bi bi-tools"></i> Cleanup</button>';
        }
    }
    mainContent += '        </div>';
    mainContent += '    </div>';
    mainContent += '</div>';

    return mainContent;
}

let calculateScheduler = (key, schedulerModel) => {
    let mainContent = '';

    mainContent += '<div class="container mt-2 mb-2 border rounded border-color-gray">';
    mainContent += '    <div class="row mt-1">';
    mainContent += '        <div class="col-12">';
    mainContent += '            <span class="h5">Scheduler</span>';
    mainContent += '        </div>';
    mainContent += '    </div>';
    mainContent += '    <div class="row mt-1">';
    mainContent += '        <div class="col-3">Name</div>';
    mainContent += '        <div class="col-9">' + schedulerModel.schedulerName + '</div>';
    mainContent += '    </div>';
    mainContent += '    <div class="row mt-1">';
    mainContent += '        <div class="col-3 h6">Trigger</div>';
    mainContent += '        <div class="col-9">';
    schedulerModel.triggers.forEach(triggerModel => mainContent += calculateTrigger(key, triggerModel));
    mainContent += '        </div>';
    mainContent += '    </div>';
    mainContent += '</div>';
    return mainContent;
}

let calculateStatistics = (key, values) => {
    let mainContent = ''
    mainContent += '<div class="container mt-2 mb-2 border rounded border-color-gray">';
    mainContent += '    <div class="row mt-1">';
    mainContent += '        <div class="col-12">';
    mainContent += '            <span class="h5">Gruppe</span>';
    mainContent += '        </div>';
    mainContent += '    </div>';
    mainContent += '    <div class="row mt-1">';
    mainContent += '        <div class="col-3">Name</div>';
    mainContent += '        <div class="col-9"' + key + '</div>';
    mainContent += '    </div>';
    values.forEach(schedulerModel => mainContent += calculateScheduler(key, schedulerModel));
    mainContent += '</div>';
    return mainContent;
};

let renderAdminActionModal = (result) => {
    let mainContent = '';

    mainContent += '<div class="container mt-2">';
    mainContent += '    <div class="row">';
    mainContent += '        <div class="col-12">';
    mainContent += '            <span class="h5">Statistiken</span>';
    mainContent += '        </div>';
    mainContent += '    </div>';
    mainContent += '</div>';

    $.each(result, (key, values) => mainContent += calculateStatistics(values.key, values.value));

    return mainContent;
}