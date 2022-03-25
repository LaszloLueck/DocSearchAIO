let renderAdminSchedulerModal = (result) => {
    let mainContent = '';
    mainContent += ' <div class="container mt-2 border rounded border-color-gray">';
    mainContent += '     <div class="row">';
    mainContent += '         <div class="col-12">';
    mainContent += '             <span class="h5">Scheduler</span>';
    mainContent += '         </div>';
    mainContent += '     </div>';
    $.each(result, function (key, schedulerStatistics) {
        mainContent += '         <div class="container mt-2 border rounded border-color-gray">';
        mainContent += '             <div class="row">';
        mainContent += '                 <div class="col-12">';
        mainContent += '                     <span class="h5">Gruppe ' + schedulerStatistics.key + '</span>';
        mainContent += '                 </div>';
        mainContent += '             </div>';
        let badge = '<span class="badge badge-secondary">Unknown state</span>';
        switch (schedulerStatistics.value.state.toLowerCase()) {
            case "gestartet":
                badge = '<span class="badge badge-success">' + schedulerStatistics.value.state + '</span>';
                break;
            case "gestoppt":
                badge = '<span class="badge badge-danger">' + schedulerStatistics.value.state + '</span>';
                break;
            case "pausiert":
                badge = '<span class="badge badge-warning">' + schedulerStatistics.value.state + '</span>';
                break;
            case "unbekannt":
                badge = '<span class="badge badge-secondary">' + schedulerStatistics.value.state + '</span>';
                break;
        }
        mainContent += '             <div class="row">';
        mainContent += '             <div class="col-3">';
        mainContent += '             <span class="h6">' + schedulerStatistics.value.schedulerName + '</span>';
        mainContent += '             </div>';
        mainContent += '             <div class="col-9">' + badge + '</div>';
        mainContent += '             </div>';
        mainContent += '             <div class="row mb-2">';
        mainContent += '             <div class="col-3">Instance-Id</div>';
        mainContent += '             <div class="col-9">' + schedulerStatistics.value.schedulerInstanceId + '</div>';
        mainContent += '             </div>';
        mainContent += '             <div class="row">';
        mainContent += '             <div class="col-12">';
        mainContent += '             <span class="h5">Trigger</span>';
        mainContent += '             </div>';
        mainContent += '             </div>';
        schedulerStatistics.value.triggerElements.forEach(trigger => {
            let triggerBatch = '<span class="badge badge-secondary">Unbekannt</span>"';
            switch (trigger.triggerState.toLowerCase()) {
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


            mainContent += '             <div class="row border-top border-color-gray">';
            mainContent += '             <div class="col-1">&nbsp;</div>';
            mainContent += '             <div class="col-3">';
            mainContent += '             <span class="h6">' + trigger.triggerName + '</span>';
            mainContent += '             </div>';
            mainContent += '             <div class="col-8">' + triggerBatch + '</div>';
            mainContent += '             </div>';
            mainContent += '             <div class="row">';
            mainContent += '             <div class="col-1">&nbsp;</div>';
            mainContent += '             <div class="col-3">Jobname</div>';
            mainContent += '             <div class="col-8">' + trigger.jobName + '</div>';
            mainContent += '             </div>';
            mainContent += '             <div class="row">';
            mainContent += '             <div class="col-1">&nbsp;</div>';
            mainContent += '             <div class="col-3">Beschreibung</div>';
            mainContent += '             <div class="col-8">' + trigger.description + '</div>';
            mainContent += '             </div>';
            mainContent += '             <div class="row">';
            mainContent += '             <div class="col-1">&nbsp;</div>';
            mainContent += '             <div class="col-3">Gruppe</div>';
            mainContent += '             <div class="col-8">' + trigger.groupName + '</div>';
            mainContent += '             </div>';
            mainContent += '             <div class="row">';
            mainContent += '             <div class="col-1">&nbsp;</div>';
            mainContent += '             <div class="col-3">Startzeit</div>';
            mainContent += '             <div class="col-8">' + trigger.startTime + '</div>';
            mainContent += '             </div>';
            mainContent += '             <div class="row">';
            mainContent += '             <div class="col-1">&nbsp;</div>';
            mainContent += '             <div class="col-3">Letzte Ausführung</div>';
            mainContent += '             <div class="col-8">' + trigger.lastFireTime + '</div>';
            mainContent += '             </div>';
            mainContent += '             <div class="row mb-2">';
            mainContent += '             <div class="col-1">&nbsp;</div>';
            mainContent += '             <div class="col-3">Nächster Start</div>';
            mainContent += '             <div class="col-8">' + trigger.nextFireTime + '</div>';
            mainContent += '             </div>';
        });
        mainContent += '         </div>';
    });
    mainContent += ' </div>';
    return mainContent;
}