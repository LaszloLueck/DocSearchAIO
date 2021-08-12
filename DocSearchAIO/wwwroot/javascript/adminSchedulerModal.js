renderAdminSchedulerModal = (result) => {
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
        mainContent += '                     <span class="h5">Gruppe ' + key + '</span>';
        mainContent += '                 </div>';
        mainContent += '             </div>';
        let badge = '<span class="badge badge-secondary">Unknown state</span>';
        switch (schedulerStatistics.state.toLowerCase()) {
            case "gestartet":
                badge = '<span class="badge badge-success">' + schedulerStatistics.state + '</span>';
                break;
            case "gestoppt":
                badge = '<span class="badge badge-danger">' + schedulerStatistics.state + '</span>';
                break;
            case "pausiert":
                badge = '<span class="badge badge-warning">' + schedulerStatistics.state + '</span>';
                break;
            case "unbekannt":
                badge = '<span class="badge badge-secondary">' + schedulerStatistics.state + '</span>';
                break;
        }
        mainContent += '             <div class="row">';
        mainContent += '             <div class="col-3">';
        mainContent += '             <span class="h6">' + schedulerStatistics.schedulerName + '</span>';
        mainContent += '             </div>';
        mainContent += '             <div class="col-9">' + badge + '</div>';
        mainContent += '             </div>';
        mainContent += '             <div class="row mb-2">';
        mainContent += '             <div class="col-3">Instance-Id</div>';
        mainContent += '             <div class="col-9">' + schedulerStatistics.schedulerInstanceId + '</div>';
        mainContent += '             </div>';
        mainContent += '             <div class="row">';
        mainContent += '             <div class="col-12">';
        mainContent += '             <span class="h5">Trigger</span>';
        mainContent += '             </div>';
        mainContent += '             </div>';
        schedulerStatistics.triggerElements.forEach(trigger => {
            // mainContent += '             var triggerBatch = trigger.TriggerState.ToLower() switch{';
            // mainContent += '             "blocked" => "<span class=\"badge badge-warning\">Blockiert</span>",';
            // mainContent += '             "complete" => "<span class=\"badge badge-secondary\">Komplett</span>",';
            // mainContent += '             "error" => "<span class=\"badge badge-danger\">Fehler</span>",';
            // mainContent += '             "none" => "<span class=\"badge badge-info\">Kein Status</span>",';
            // mainContent += '             "normal" => "<span class=\"badge badge-success\">Normal</span>",';
            // mainContent += '             "paused" => "<span class=\"badge badge-primary\">Pausiert</span>",';
            // mainContent += '             _ => "span class=\"badge badge-secondary\">Unbekannt</span>"';
            // mainContent += '         };';
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
            mainContent += '             <div class="col-8">' + trigger.NextFireTime + '</div>';
            mainContent += '             </div>';
        });
        mainContent += '         </div>';
    });
    mainContent += ' </div>';
    return mainContent;
}