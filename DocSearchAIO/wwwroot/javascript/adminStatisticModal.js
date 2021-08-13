renderAdminStatisticsModal = (result) => {
    let mainContent = '';

    mainContent += '<div class="container mt-2">';
    mainContent += '    <div class="row">';
    mainContent += '        <div class="col-12">';
    mainContent += '            <span class="h5">Statistiken</span>';
    mainContent += '        </div>';
    mainContent += '    </div>';
    mainContent += '</div>';
    mainContent += '<div class="container mt-2 border rounded border-color-gray">';
    mainContent += '    <div class="row mt-1">';
    mainContent += '        <div class="col-12">';
    mainContent += '            <span class="h5">Allgemein</span>';
    mainContent += '        </div>';
    mainContent += '    </div>';
    mainContent += '    <div class="row mt-1">';
    mainContent += '        <div class="col-3">Gesamtanzahl Dokumente</div>';
    mainContent += '        <div class="col-9">' + result.entireDocCount + '</div>';
    mainContent += '    </div>';
    mainContent += '    <div class="row mt-1">';
    mainContent += '        <div class="col-3">Gesamte Indexgröße in Byte</div>';
    mainContent += '        <div class="col-9">' + result.entireSizeInBytes + '</div>';
    mainContent += '    </div>';
    mainContent += '</div>';
    mainContent += '<div class="container mt-2 border rounded border-color-gray">';
    mainContent += '    <div class="row mt-1">';
    mainContent += '        <div class="col-12">';
    mainContent += '            <span class="h5">Letzte Ausführung</span>';
    mainContent += '        </div>';
    mainContent += '    </div>';
    $.each(result.runtimeStatistics, function (key, value) {
        mainContent += '        <div class="row">';
        mainContent += '            <div class="col-12">';
        mainContent += '                <span class="h5">@runtimeModel.Key</span>';
        mainContent += '            </div>';
        mainContent += '        </div>';
        mainContent += '        <div class="row">';
        mainContent += '        <div class="col-3">';
        mainContent += '        <span>Id:</span>';
        mainContent += '        </div>';
        mainContent += '        <div class="col-9">';
        mainContent += '        <span>@runtimeModel.Value.Id</span>';
        mainContent += '        </div>';
        mainContent += '        </div>';
        mainContent += '        <div class="row">';
        mainContent += '        <div class="col-3">';
        mainContent += '        <span>Startzeit:</span>';
        mainContent += '        </div>';
        mainContent += '        <div class="col-9">';
        mainContent += '        <span>@($"{runtimeModel.Value.StartJob:dd.MM.yyyy HH:mm:ss}")</span>';
        mainContent += '        </div>';
        mainContent += '        </div>';
        mainContent += '        <div class="row">';
        mainContent += '        <div class="col-3">';
        mainContent += '        <span>Endezeit:</span>';
        mainContent += '        </div>';
        mainContent += '        <div class="col-9">';
        mainContent += '        <span>@($"{runtimeModel.Value.EndJob:dd.MM.yyyy HH:mm:ss}")</span>';
        mainContent += '        </div>';
        mainContent += '        </div>';
        mainContent += '        <div class="row">';
        mainContent += '        <div class="col-3">';
        mainContent += '        <span>Dokumente verarbeitet:</span>';
        mainContent += '        </div>';
        mainContent += '        <div class="col-9">';
        mainContent += '        <span>@runtimeModel.Value.EntireDocCount</span>';
        mainContent += '        </div>';
        mainContent += '        </div>';
        mainContent += '        <div class="row">';
        mainContent += '        <div class="col-3">';
        mainContent += '        <span>Dokumente mit Fehlern:</span>';
        mainContent += '        </div>';
        mainContent += '        <div class="col-9">';
        mainContent += '        <span>@runtimeModel.Value.ProcessingError</span>';
        mainContent += '        </div>';
        mainContent += '        </div>';
        mainContent += '        <div class="row">';
        mainContent += '        <div class="col-3">';
        mainContent += '        <span>Dokumente indexiert:</span>';
        mainContent += '        </div>';
        mainContent += '        <div class="col-9">';
        mainContent += '        <span>@runtimeModel.Value.IndexedDocCount</span>';
        mainContent += '        </div>';
        mainContent += '        </div>';
        mainContent += '        <div class="row">';
        mainContent += '        <div class="col-3">';
        mainContent += '        <span>Dokumente verarbeitet/s:</span>';
        mainContent += '        </div>';
        mainContent += '        @{';
        mainContent += '        if (runtimeModel.Value.ElapsedTimeMillis > 0 && runtimeModel.Value.EntireDocCount > 0 && runtimeModel.Value.ElapsedTimeMillis / 1000 > 0)';
        mainContent += '    {';
        mainContent += '        <div class="col-9">';
        mainContent += '        <span>@(runtimeModel.Value.EntireDocCount / (runtimeModel.Value.ElapsedTimeMillis / 1000))</span>';
        mainContent += '        </div>';
        mainContent += '    }';
        mainContent += '        else';
        mainContent += '    {';
        mainContent += '        <div class="col-9">';
        mainContent += '        <span>0</span>';
        mainContent += '        </div>';
        mainContent += '    }';
        mainContent += '    }';
        mainContent += '        </div>';
        mainContent += '        @if (runtimeModel.Value.CacheEntry.HasValue)';
        mainContent += '    {';
        mainContent += '        <div class="row">';
        mainContent += '        <div class="col-3">';
        mainContent += '        <span>In Memory Cache Status</span>';
        mainContent += '        </div>';
        mainContent += '        <div class="col-9">';
        mainContent += '        <span>@runtimeModel.Value.CacheEntry.Value.JobState.ToString()</span>';
        mainContent += '        </div>';
        mainContent += '        </div>';
        mainContent += '        <div class="row">';
        mainContent += '        <div class="col-3">';
        mainContent += '        <span>In Memory Cache Zeit</span>';
        mainContent += '        </div>';
        mainContent += '        <div class="col-9">';
        mainContent += '        <span>@runtimeModel.Value.CacheEntry.Value.DateTime</span>';
        mainContent += '        </div>';
        mainContent += '        </div>';
    });
    mainContent += '</div>';
    mainContent += '<div class="container mt-2 border rounded border-color-gray">';
    mainContent += '    <div class="row mt-1">';
    mainContent += '        <div class="col-12">';
    mainContent += '            <span class="h5">Pro Index</span>';
    mainContent += '        </div>';
    mainContent += '    </div>';
    mainContent += '    @foreach (var indexStatisticModel in Model.IndexStatisticModels)';
    mainContent += '    {';
    mainContent += '        <div class="row">';
    mainContent += '            <div class="col-12">';
    mainContent += '                <span class="h6">@indexStatisticModel.IndexName</span>';
    mainContent += '            </div>';
    mainContent += '        </div>';
    mainContent += '        <div class="row mb-2">';
    mainContent += '        <div class="col-3">Anzahl Dokumente</div>';
    mainContent += '        <div class="col-9">@indexStatisticModel.DocCount</div>';
    mainContent += '        </div>';
    mainContent += '        <div class="row mb-2">';
    mainContent += '        <div class="col-3">Größe ind Bytes</div>';
    mainContent += '        <div class="col-9">@($"{indexStatisticModel.SizeInBytes:0,0}")</div>';
    mainContent += '        </div>';
    mainContent += '        <div class="row mb-2">';
    mainContent += '        <div class="col-3">Fetch-Time in ms</div>';
    mainContent += '        <div class="col-9">@(indexStatisticModel.FetchTimeMs)</div>';
    mainContent += '        </div>';
    mainContent += '        <div class="row mb-2">';
    mainContent += '        <div class="col-3">Fetches total</div>';
    mainContent += '        <div class="col-9">@(indexStatisticModel.FetchTotal)</div>';
    mainContent += '        </div>';
    mainContent += '        <div class="row mb-2">';
    mainContent += '        <div class="col-3">Suggest-Time in ms</div>';
    mainContent += '        <div class="col-9">@(indexStatisticModel.SuggestTimeMs)</div>';
    mainContent += '        </div>';
    mainContent += '        <div class="row mb-2">';
    mainContent += '        <div class="col-3">Suggests total</div>';
    mainContent += '        <div class="col-9">@(indexStatisticModel.SuggestTotal)</div>';
    mainContent += '        </div>';
    mainContent += '        <div class="row mb-2">';
    mainContent += '        <div class="col-3">Query-Time in ms</div>';
    mainContent += '        <div class="col-9">@(indexStatisticModel.QueryTimeMs)</div>';
    mainContent += '        </div>';
    mainContent += '        <div class="row mb-2">';
    mainContent += '        <div class="col-3">Queries total</div>';
    mainContent += '        <div class="col-9">@(indexStatisticModel.QueryTotal)</div>';
    mainContent += '        </div>';

    mainContent += '</div>';

    return mainContent;
}