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


        if(element.contents.length > 0) {
            mainContent += '<div class="mb-1">';
            mainContent += '<div>';
            mainContent += '<small class="text-muted">Inhalt</small>';
            mainContent += '</div>';
            mainContent += '<ul class="list-group">';
            element.contents.forEach(content => {
                mainContent += '<li class="list-group-item py-2">';
                mainContent += '<small>' + content.contentText + '</small>';
                mainContent += '</li>';
            });
            mainContent += '</ul>';
            mainContent += '</div>';
        }

        if(element.comments.length > 0) {
            mainContent += '<div class="mb-1">';
            mainContent += '<div>';
            mainContent += '<small class="text-muted">Kommentar</small>';
            mainContent += '</div>';
            mainContent += '<ul class="list-group">';
            element.comments.forEach(comment => {
                mainContent += '<li class="list-group-item py-2">';
                mainContent += '<div><small>Autor: ' + comment.author + ' | Datum: ' + comment.date + '</small></div>'
                mainContent += '<div><small>' + comment.commentText + '</small></div>';
                mainContent += '</li>';
            });
            mainContent += '</ul>';
            mainContent += '</div>';
        }

        mainContent += '<h6>';
        mainContent += '<small class="text-muted">Relevanz: ' + element.relevance + ' | Id: ' + element.id + '</small>';
        mainContent += '</h6>';
        mainContent += '</div>';
    });
    return mainContent;
}
//escapeMarkup(content)