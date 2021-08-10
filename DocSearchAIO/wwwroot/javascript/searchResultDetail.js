renderSearchResultDetails = (detailElements) => {
    let mainContent = '';
    detailElements.forEach(element => {
        mainContent += '<div class="w-100 mb-2">';
        mainContent += '<h4 class="mb-0">';
        mainContent += '<img class="mr-1" src="' + element.programIcon + '" height="35" width="35" alt="ProgramIcon" />';
        mainContent += '<img class="mr-1" style="cursor:pointer" src="images/info.svg" alt="info" height="32" width="32" id="info_' + element.id + '" onclick="openDocumentDetailDialog(\'' + element.id + '\')" />';
        mainContent += '<a href="' + element.relativeUrl + '">';
        mainContent += '<small>' + element.relativeUrl + '</small>';
        mainContent += '</a>';
        mainContent += '</h4>';
        
        mainContent += '<h5>';
        mainContent += '<small class="mr-1">Download</small>';
        mainContent += '<a href="/api/base/download?path=' + element.absoluteUrl + '&documentType=' + element.documentType + '" target="_blank">';
        mainContent += '<small>' + element.absoluteUrl + '</small>';
        mainContent += '</a>';
        mainContent += '</h5>';
        
        element.searchBody.forEach(bodyElement => {
            mainContent += '<div class="mb-1">';
            mainContent += '<div>';
            mainContent += '<small class="text-muted">' + bodyElement.contentType + ': </small>';
            mainContent += '</div>';
            mainContent += '<ul class="list-group">';
            bodyElement.contentValues.forEach(content => {
               mainContent += '<li class="list-group-item py-2">';
               mainContent += '<small>' + content + '</small>';
               mainContent += '</li>';
            });
            mainContent += '</ul>';
            mainContent += '</div>';
        });
        
        mainContent += '<h6>';
        mainContent += '<small class="text-muted">Relevanz: ' + element.relevance + ' | Id: ' + element.id + '</small>';
        mainContent += '</h6>';
        mainContent += '</div>';
    });
    return mainContent;
}