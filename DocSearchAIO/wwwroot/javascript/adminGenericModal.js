let renderAdminGenericModal = (result) => {
    let mainContent = '<div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
    mainContent += '<div class="input-group input-group-sm m-2">';
    mainContent += '<div class="input-group-prepend">';
    mainContent += '<span class="input-group-text" id="labelState">Scan-Pfad</span>';
    mainContent += '</div>';
    mainContent += '<input type="text" class="form-control" id="scanPath" value="' + result.scanPath + '">';
    mainContent += '</div>';
    mainContent += '</div>';

    let dictionaryLength = result.elasticEndpoints.length;
    
    for (let i = 0; i < dictionaryLength; i++) {
        mainContent += '<div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray" id="elasticNodeContainer">';
        mainContent += '<div class="input-group input-group-sm m-2" id="elasticEndpoint_' + i + '">';
        mainContent += '<div class="input-group-prepend">';
        mainContent += '<span class="input-group-text" id="labelElasticEndpoint_' + i + '">Elastic-Endpunkt ' + i + 1 + '</span>';
        mainContent += '</div>';
        mainContent += '<input type="text" class="form-control mr-1" id="elastic_endpoint_' + i + '" value="' + result.elasticEndpoints[i] + '">';
        if (i + 1 === dictionaryLength) {
            mainContent += '<button type="button" class="btn btn-sm btn-primary" onClick="addNewElasticNode()"><i class="bi bi-plus"></i> Neu</button>';
        } else {
            mainContent += '<button type="button" class="btn btn-sm btn-danger" onclick="removeNodeEntry(' + i + ')"><i class="bi bi-trash"></i> Entfernen</button>';
        }
        mainContent += '</div>';
        mainContent += '</div>';
    }

    mainContent += '<div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
    mainContent += '<div class="input-group input-group-sm m-2">';
    mainContent += '<div class="input-group-prepend">';
    mainContent += '<span class="input-group-text" id="labelIndexName">Index-Name (Prefix)</span>';
    mainContent += '</div>';
    mainContent += '<input type="text" class="form-control" id="indexName" value="' + result.indexName + '">';
    mainContent += '</div>';
    mainContent += '</div>';

    mainContent += '<div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
    mainContent += '<div class="input-group input-group-sm m-2">';
    mainContent += '<div class="input-group-prepend">';
    mainContent += '<span class="input-group-text" id="labelSchedulerName">Scheduler-Name</span>';
    mainContent += '</div>';
    mainContent += '<input type="text" class="form-control" id="schedulerName" value="' + result.schedulerName + '">';
    mainContent += '</div>';
    mainContent += '</div>';

    mainContent += '<div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
    mainContent += '    <div class="input-group input-group-sm m-2">';
    mainContent += '        <div class="input-group-prepend">';
    mainContent += '            <span class="input-group-text" id="labelSchedulerId">Scheduler-Id</span>';
    mainContent += '        </div>';
    mainContent += '        <input type="text" class="form-control" id="schedulerId" value="' + result.schedulerId + '">';
    mainContent += '    </div>';
    mainContent += '</div>';

    mainContent += '<div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
    mainContent += '    <div class="input-group input-group-sm m-2">';
    mainContent += '        <div class="input-group-prepend">';
    mainContent += '            <span class="input-group-text" id="labelActorSystemName">Actorsystem-Name</span>';
    mainContent += '        </div>';
    mainContent += '        <input type="text" class="form-control" id="actorSystemName" value="' + result.actorSystemName + '">';
    mainContent += '    </div>';
    mainContent += '</div>';

    mainContent += '<div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
    mainContent += '    <div class="input-group input-group-sm m-2">';
    mainContent += '        <div class="input-group-prepend">';
    mainContent += '            <span class="input-group-text" id="labelGroupName">Schedulerprozess Gruppen-Name</span>';
    mainContent += '        </div>';
    mainContent += '        <input type="text" class="form-control" id="groupName" value="' + result.processorGroupName + '">';
    mainContent += '    </div>';
    mainContent += '</div>';

    mainContent += '<div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
    mainContent += '    <div class="input-group input-group-sm m-2">';
    mainContent += '        <div class="input-group-prepend">';
    mainContent += '            <span class="input-group-text" id="labelCleanupGroupName">Cleanup Gruppen-Name</span>';
    mainContent += '        </div>';
    mainContent += '        <input type="text" class="form-control" id="cleanupGroupName" value="' + result.cleanupGroupName + '">';
    mainContent += '    </div>';
    mainContent += '</div>';

    mainContent += '<div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
    mainContent += '<div class="input-group input-group-sm m-2">';
    mainContent += '<div class="input-group-prepend">';
    mainContent += '<span class="input-group-text" id="labelUrlReplacement">Url-Ersetzung</span>';
    mainContent += '</div>';
    mainContent += '<input type="text" class="form-control" id="urlReplacement" value="' + result.uriReplacement + '">';
    mainContent += '</div>';
    mainContent += '</div>';

    mainContent += '<div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
    mainContent += '    <div class="input-group input-group-sm m-2">';
    mainContent += '        <div class="input-group-prepend">';
    mainContent += '            <span class="input-group-text" id="labelComparerDirectory">Comparer-Verzeichnis</span>';
    mainContent += '        </div>';
    mainContent += '        <input type="text" class="form-control" id="comparerDirectory" value="' + result.comparerDirectory + '">';
    mainContent += '    </div>';
    mainContent += '</div>';

    mainContent += '<div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
    mainContent += '    <div class="input-group input-group-sm m-2">';
    mainContent += '        <div class="input-group-prepend">';
    mainContent += '            <span class="input-group-text" id="labelStatisticsDirectory">Statistik-Verzeichnis</span>';
    mainContent += '        </div>';
    mainContent += '        <input type="text" class="form-control" id="statisticsDirectory" value="' + result.statisticsDirectory + '">';
    mainContent += '    </div>';
    mainContent += '</div>';

    mainContent += '<div class="ml-1 mr-1 mt-2 mb-2 w-100">';
    mainContent += '    <div class="row">';
    mainContent += '        <div class="col-12">';
    mainContent += '            <span class="h6">Prozess-Jobs Einstellungen</span>';
    mainContent += '        </div>';
    mainContent += '    </div>';
    mainContent += '</div>';

    $.each(result.processorConfigurations, function(key, value){
        mainContent += '<div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray" id="processorType_' + key + '">';
        mainContent += '    <div class="input-group input-group-sm m-2">';
        mainContent += '        <div class="h6 mr-2">';
        mainContent += '        <span>';
        mainContent += '            <strong>Job</strong>';
        mainContent += '        </span>';
        mainContent += '        </div>';
        mainContent += '        <div class="h6">';
        mainContent += '            <span>' + key + '</span>';
        mainContent += '        </div>';
        mainContent += '    </div>';
        mainContent += '    <div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
        mainContent += '        <div class="input-group input-group-sm m-2">';
        mainContent += '            <div class="input-group-prepend">';
        mainContent += '                <span class="input-group-text" id="' + key + '_labelStartEveryPrepend">Start alle</span>';
        mainContent += '            </div>';
        mainContent += '            <input type="text" class="form-control" id="' + key + '_runsEvery" value="' + value.runsEvery + '">';
        mainContent += '            <div class="input-group-append">';
        mainContent += '                <span class="input-group-text" id="' + key + '_labelStartEveryAppend">Sekunden</span>';
        mainContent += '            </div>';
        mainContent += '        </div>';
        mainContent += '    </div>';
        mainContent += '    <div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
        mainContent += '        <div class="input-group input-group-sm m-2">';
        mainContent += '            <div class="input-group-prepend">';
        mainContent += '                <span class="input-group-text" id="' + key + '_labelStartDelayPrepend">Startverz??gerung</span>';
        mainContent += '            </div>';
        mainContent += '            <input type="text" class="form-control" id="' + key + '_startDelay" value="' + value.startDelay + '">';
        mainContent += '            <div class="input-group-append">';
        mainContent += '                <span class="input-group-text" id="' + key + '_labelStartDelayAppend">Sekunden</span>';
        mainContent += '            </div>';
        mainContent += '        </div>';
        mainContent += '    </div>';
        mainContent += '    <div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
        mainContent += '        <div class="input-group input-group-sm m-2">';
        mainContent += '            <div class="input-group-prepend">';
        mainContent += '                <span class="input-group-text" id="' + key + '_labelParallelismPrepend">Ausf??hrungs-Parallelit??t</span>';
        mainContent += '            </div>';
        mainContent += '            <input type="text" class="form-control" id="' + key + '_parallelism" value="' + value.parallelism + '">';
        mainContent += '            <div class="input-group-append">';
        mainContent += '                <span class="input-group-text" id="' + key + '_labelParallelismAppend">Threads</span>';
        mainContent += '            </div>';
        mainContent += '        </div>';
        mainContent += '    </div>';
        mainContent += '    <div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
        mainContent += '        <div class="input-group input-group-sm m-2">';
        mainContent += '            <div class="input-group-prepend">';
        mainContent += '                <span class="input-group-text" id="' + key + '_labelFileExtensionPrepend">Dateierweiterung</span>';
        mainContent += '            </div>';
        mainContent += '            <input type="text" class="form-control" id="' + key + '_fileExtension" value="' + value.fileExtension + '">';
        mainContent += '        </div>';
        mainContent += '    </div>';
        mainContent += '    <div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
        mainContent += '        <div class="input-group input-group-sm m-2">';
        mainContent += '            <div class="input-group-prepend">';
        mainContent += '                <span class="input-group-text" id="' + key + '_labelFileFilterPrepend">Dateifilter Ausschluss</span>';
        mainContent += '            </div>';
        mainContent += '            <input type="text" class="form-control" id="' + key + '_fileFilter" value="' + value.excludeFilter + '">';
        mainContent += '        </div>';
        mainContent += '    </div>';
        mainContent += '    <div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
        mainContent += '        <div class="input-group input-group-sm m-2">';
        mainContent += '            <div class="input-group-prepend">';
        mainContent += '                <span class="input-group-text" id="' + key + '_labelSchedulerJobNamePrepend">Scheduler Jobname</span>';
        mainContent += '            </div>';
        mainContent += '            <input type="text" class="form-control" id="' + key + '_schedulerJobName" value="' + value.jobName + '">';
        mainContent += '        </div>';
        mainContent += '    </div>';
        mainContent += '    <div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
        mainContent += '        <div class="input-group input-group-sm m-2">';
        mainContent += '            <div class="input-group-prepend">';
        mainContent += '                <span class="input-group-text" id="' + key + '_labelSchedulerTriggerNamePrepend">Scheduler Triggername</span>';
        mainContent += '            </div>';
        mainContent += '            <input type="text" class="form-control" id="' + key + '_schedulerTriggerName" value="' + value.triggerName + '">';
        mainContent += '        </div>';
        mainContent += '    </div>';
        mainContent += '    <div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
        mainContent += '        <div class="input-group input-group-sm m-2">';
        mainContent += '            <div class="input-group-prepend">';
        mainContent += '                <span class="input-group-text" id="' + key + '_labelIndexSuffixPrepend"">Index Suffix</span>';
        mainContent += '            </div>';
        mainContent += '            <input type="text" class="form-control" id="' + key + '_indexSuffix" value="' + value.indexSuffix + '">';
        mainContent += '        </div>';
        mainContent += '    </div>';
        mainContent += '</div>';
    });

    mainContent += '<div class="ml-1 mr-1 mt-2 mb-2 w-100">';
    mainContent += '    <div class="row">';
    mainContent += '        <div class="col-12">';
    mainContent += '            <span class="h6">Cleanup-Jobs Einstellungen</span>';
    mainContent += '        </div>';
    mainContent += '    </div>';
    mainContent += '</div>';

    $.each(result.cleanupConfigurations, function(key, value) {
        mainContent += '<div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray" id="cleanupType_' + key + '">';
        mainContent += '    <div class="input-group input-group-sm m-2">';
        mainContent += '        <div class="h6 mr-2">';
        mainContent += '        <span>';
        mainContent += '            <strong>Job</strong>';
        mainContent += '        </span>';
        mainContent += '        </div>';
        mainContent += '        <div class="h6">';
        mainContent += '            <span>' + key + '</span>';
        mainContent += '        </div>';
        mainContent += '    </div>';
        mainContent += '    <div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
        mainContent += '        <div class="input-group input-group-sm m-2">';
        mainContent += '            <div class="input-group-prepend">';
        mainContent += '                <span class="input-group-text" id="' + key + '_labelStartEveryPrepend">Start alle</span>';
        mainContent += '            </div>';
        mainContent += '            <input type="text" class="form-control" id="' + key + '_runsEvery" value="' + value.runsEvery + '">';
        mainContent += '            <div class="input-group-append">';
        mainContent += '                <span class="input-group-text" id="' + key + '_labelStartEveryAppend">Sekunden</span>';
        mainContent += '            </div>';
        mainContent += '        </div>';
        mainContent += '    </div>';
        mainContent += '    <div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
        mainContent += '        <div class="input-group input-group-sm m-2">';
        mainContent += '            <div class="input-group-prepend">';
        mainContent += '                <span class="input-group-text" id="' + key + '_labelStartDelayPrepend">Startverz??gerung</span>';
        mainContent += '            </div>';
        mainContent += '            <input type="text" class="form-control" id="' + key + '_startDelay" value="' + value.startDelay + '">';
        mainContent += '            <div class="input-group-append">';
        mainContent += '                <span class="input-group-text" id="' + key + '_labelStartDelayAppend">Sekunden</span>';
        mainContent += '            </div>';
        mainContent += '        </div>';
        mainContent += '    </div>';
        mainContent += '    <div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
        mainContent += '        <div class="input-group input-group-sm m-2">';
        mainContent += '            <div class="input-group-prepend">';
        mainContent += '                <span class="input-group-text" id="' + key + '_labelStartEveryPrepend">Ausf??hrungs-Parallelit??t</span>';
        mainContent += '            </div>';
        mainContent += '            <input type="text" class="form-control" id="' + key + '_parallelism" value="' + value.parallelism + '">';
        mainContent += '            <div class="input-group-append">';
        mainContent += '                <span class="input-group-text" id="' + key + '_labelStartEveryAppend">Threads</span>';
        mainContent += '            </div>';
        mainContent += '        </div>';
        mainContent += '    </div>';
        mainContent += '    <div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
        mainContent += '        <div class="input-group input-group-sm m-2">';
        mainContent += '            <div class="input-group-prepend">';
        mainContent += '                <span class="input-group-text" id="' + key + '_labelCleanupJobNamePrepend">Cleanup-Jobname</span>';
        mainContent += '            </div>';
        mainContent += '            <input type="text" class="form-control" id="' + key + '_cleanupJobName" value="' + value.jobName + '">';
        mainContent += '        </div>';
        mainContent += '    </div>';
        mainContent += '    <div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
        mainContent += '        <div class="input-group input-group-sm m-2">';
        mainContent += '            <div class="input-group-prepend">';
        mainContent += '                <span class="input-group-text" id="' + key + '_labelCleanupTriggerNamePrepend">Cleanup-Triggername</span>';
        mainContent += '            </div>';
        mainContent += '            <input type="text" class="form-control" id="' + key + '_cleanupTriggerName" value="' + value.triggerName + '">';
        mainContent += '        </div>';
        mainContent += '    </div>';
        mainContent += '    <div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
        mainContent += '        <div class="input-group input-group-sm m-2">';
        mainContent += '            <div class="input-group-prepend">';
        mainContent += '               <span class="input-group-text" id="' + key + '_labelCleanupForComparerPrepend">Prozess-Comparer-Dateiname</span>';
        mainContent += '            </div>';
        mainContent += '            <input type="text" class="form-control" id="' + key + '_cleanupForComparerName" value="' + value.forComparer + '">';
        mainContent += '        </div>';
        mainContent += '    </div>';
        mainContent += '    <div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
        mainContent += '        <div class="input-group input-group-sm m-2">';
        mainContent += '            <div class="input-group-prepend">';
        mainContent += '                <span class="input-group-text" id="' + key + '_labelCleanupForIndexSuffixPrepend">Prozess-Indexname</span>';
        mainContent += '            </div>';
        mainContent += '            <input type="text" class="form-control" id="' + key + '_cleanupForIndexSuffix" value="' + value.forIndexSuffix + '">';
        mainContent += '        </div>';
        mainContent += '    </div>';
        mainContent += '</div>';
    });


    mainContent += '<div class="form-row ml-1 mr-1 mt-2 mb-2 w-100 border rounded border-color-gray">';
    mainContent += '    <div class="input-group m-2 d-flex justify-content-end">';
    mainContent += '        <button type="button" class="btn btn-primary" onClick="saveAdminContent();">Speichern</button>';
    mainContent += '    </div>';
    mainContent += '</div>';

    return mainContent;
}