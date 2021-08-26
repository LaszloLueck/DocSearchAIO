
let renderContentGroup = (contents) => {
    let mainContent = '';

    if(contents.length > 0) {
        mainContent += '<div class="mb-1">';
        mainContent += '<div>';
        mainContent += '<small class="text-muted">Inhalt</small>';
        mainContent += '</div>';
        mainContent += '<ul class="list-group">';
        contents.forEach(content => {
            mainContent += '<li class="list-group-item py-2">';
            mainContent += '<small>' + content.contentText + '</small>';
            mainContent += '</li>';
        });
        mainContent += '</ul>';
        mainContent += '</div>';
    }
    
    return mainContent;
}

let renderAuthor = (author) => {
    return typeof author !== "undefined" ? "Author: " + author + " | " : "";
}
let renderDate = (date) => {
    return typeof date !== "undefined" ? "Datum: " + luxon.DateTime.fromISO(date).toFormat('dd.MM.yyyy HH:mm:ss ttt') + " | " : "";
}
let renderId = (id) => {
    return typeof id !== "undefined" ? "Id: " + id + " | " : "";
}
let renderInitials = (initials) => {
    return typeof initials !== "undefined" ? "Initiale: " + initials + " | " : "";
}

let renderComment = (author, date, id, initials) => {
    return (author + date + id + initials).slice(-2) === "| " ? "<div class='text-muted'><small>" + (author + date + id + initials).slice(0, -2) + "</small></div>" : "";
}

let renderSearchResultDetails = (detailElements) => {
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


        mainContent += renderContentGroup(element.contents);

        if(element.comments.length > 0) {
            mainContent += '<div class="mb-1">';
            mainContent += '<div>';
            mainContent += '<small class="text-muted">Kommentar</small>';
            mainContent += '</div>';
            mainContent += '<ul class="list-group">';
            element.comments.forEach(comment => {
                mainContent += '<li class="list-group-item py-2">';
                let author = renderAuthor(comment.author);
                let date = renderDate(comment.date);
                let id = renderId(comment.id);
                let initials = renderInitials(comment.initials);
                mainContent += renderInitials(author, date, id, initials); 
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