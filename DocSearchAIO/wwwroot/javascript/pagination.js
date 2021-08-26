let getModResult = (documentCount, pageSize) => (documentCount % pageSize === 0) ? 0 : 1;
let getPagingCount = (documentCount, pageSize) => documentCount <= pageSize ? 0 : (documentCount - documentCount % pageSize) / pageSize + getModResult(documentCount, pageSize);

let renderLeftPart = (currentPageNumber, pagingCount, searchPhrase, pageSize, documentCount) => {
    let mainContent = '';
    if (currentPageNumber > 1 && pagingCount > 1) {
        let link = 'javascript:doSearch(\'' + searchPhrase + '\', ' + (currentPageNumber - 2) * pageSize + ')';
        mainContent += '<li class="page-item">';
        mainContent += '<a class="page-link" href="' + link + '" aria-label="Zurück">';
        mainContent += '<span aria-hidden="true">&laquo;</span><span class="sr-only">Zurück</span>';
        mainContent += '</li>';
    } else {
        mainContent += '<li class="page-item disabled">';
        mainContent += '<div class="page-link" tabindex="-1">';
        mainContent += '<span aria-hidden="true">&laquo;</span><span class="sr-only">Zurück</span>';
        mainContent += "</div>";
        mainContent += '</li>';
    }
    if (documentCount === 0 || documentCount <= pageSize) {
        mainContent += '<li class="page-item disabled">';
        mainContent += '<div class="page-link" tabindex="-1">';
        mainContent += '<span aria-hidden="true">1</span><span class="sr-only">1</span>';
        mainContent += '</div>';
        mainContent += '</li>';
    }
    return mainContent;
}

let renderRightPart = (currentPageNumber, pagingCount, searchPhrase, pageSize) => {
    let mainContent = '';
    if (currentPageNumber < pagingCount) {
        let link = 'javascript:doSearch(\'' + searchPhrase + '\', ' + currentPageNumber * pageSize + ')';
        mainContent += '<li class="page-item">';
        mainContent += '<a class="page-link" href="' + link + '" aria-label="Vor">';
        mainContent += '<span aria-hidden="true">&raquo;</span><span class="sr-only">Vor</span>';
        mainContent += '</a>';
        mainContent += '</li>';
    } else {
        mainContent += '<li class="page-item disabled">';
        mainContent += '<div class="page-link" tabindex="-1">';
        mainContent += '<span aria-hidden="true">&raquo;</span><span class="sr-only">Vor</span>';
        mainContent += '</div>';
        mainContent += '</li>';
    }

    return mainContent;
}

let getPager = (counter, pageSize, currentPage, searchPhrase) => {

    let pager = '';
    if ((counter - 1) * pageSize === currentPage) {
        pager = '<li class="page-item active"><div class="page-link"><span aria-hidden="true">' + x + '</span></div></li>'
    } else {
        let calculatedValue = (x - 1) * pageSize;
        let link = 'javascript:doSearch(\'' + searchPhrase + '\',' + calculatedValue + ')';
        pager = '<li class="page-item"><a class="page-link" href="' + link + '">' + x + '</a></li>';
    }
    return pager;
}

let renderInnerRight = (currentPageNumber, pager, count) => {
    let mainContent = '';
    if (count === currentPageNumber + 2) {
        mainContent += pager;
        mainContent += '<li class="page-item">';
        mainContent += '<div class="page-link">';
        mainContent += '<span aria-hidden="true">&nbsp;</span>';
        mainContent += '</div>';
        mainContent += '</li>';
    }

    return mainContent;
}

let renderInnerLeft = (currentPageNumber, pager, count) => {
    let mainContent = '';
    if (count === currentPageNumber - 2) {
        mainContent += '<li class="page-item">';
        mainContent += '<div class="page-link">';
        mainContent += '<span aria-hidden="true">&nbsp;</span>';
        mainContent += '</div>';
        mainContent += '</li>';
        mainContent += pager;
    }
    return mainContent;
}

let renderInnerMiddle = (currentPageNumber, pager, count) => {
    if (count === currentPageNumber || count === currentPageNumber - 1 || count === currentPageNumber + 1) {
        return pager;
    }
}

let renderPagination = (documentCount, pageSize, currentPage, searchPhrase) => {

    let pagingCount = getPagingCount(documentCount, pageSize);
    let currentPageNumber = currentPage / pageSize + 1;

    let mainContent = '<nav aria-label="Main-Navigation">';
    mainContent += '<ul class="pagination">';

    mainContent += renderLeftPart(currentPageNumber, pagingCount, searchPhrase, pageSize, documentCount);

    for (let x = 1; x <= pagingCount; x++) {
        let pager = getPager(x, pageSize, currentPage, searchPhrase);


        if (pagingCount > 12) {
            if (x === 1 || x >= pagingCount) {
                mainContent += pager;
            } else {
                mainContent += renderInnerLeft(currentPageNumber, pager, x);
                mainContent += renderInnerMiddle(currentPageNumber, pager, x)
                mainContent += renderInnerRight(currentPageNumber, pager, x);
            }
        } else {
            mainContent += pager;
        }
    }

    mainContent += renderRightPart(currentPageNumber, pagingCount, searchPhrase, pageSize);

    mainContent += '</ul>';
    mainContent += '</nav>';


    return mainContent;
}