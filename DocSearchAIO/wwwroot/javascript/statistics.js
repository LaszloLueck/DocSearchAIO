let renderStatistics = (docCount, searchTime) => {
    let mainContent = '<div class="w-50 mx-auto d-flex justify-content-center">';
    if (docCount === 0) {
        mainContent += '<span id="suggestedDocCount">Keine Ergebnisse vorhanden (' + searchTime + ' ms)</span>';
    } else {
        mainContent += '<span id="suggestedDocCount">' + docCount + ' Dokumente</span><span class="mr-1 ml-1">|</span><span id="searchTime">Suchzeit ' + searchTime + ' ms</span>';
    }
    mainContent += '</div>';
    return mainContent;
};