let doSearch = (searchText, from) => {
    let data = {
        searchPhrase: unescape(searchText),
        from: from,
        size: localStorage.getItem("itemsPerPage"),
        filterWord: localStorage.getItem("filterWord") === "true",
        filterExcel: localStorage.getItem("filterExcel") === "true",
        filterPowerpoint: localStorage.getItem("filterPowerpoint") === "true",
        filterPdf: localStorage.getItem("filterPdf") === "true"
    };
    
    $.ajax({
        method: "POST",
        dataType: "json",
        contentType: "application/json",
        url: "/api/search/doSearch",
        data: JSON.stringify(data)
    })
        .done(function (result) {
            $('#searchField').val(result.searchResult.searchPhrase);
            if(result.searchResult.docCount === 0){
                showAlert("Keine Dokumente mit dem Suchbegriff " + result.searchResult.searchPhrase + " gefunden!", "alert-warning")
            }
            let searchResults = $('#searchResults');
            $(searchResults).empty();
            let inner = renderSearchResultDetails(result.searchResults);
            
            $(searchResults).html(inner);

            document.title = 'Doc.Search - Ihre Suche nach ' + result.searchResult.searchPhrase;

            let pagination = renderPagination(result.searchResult.docCount, result.searchResult.currentPageSize, result.searchResult.currentPage, result.searchResult.searchPhrase);
            
            
            $("#pagination")
                .empty()
                .append(pagination);
            
            let statistics = renderStatistics(result.statistics.docCount, result.statistics.searchTime);
            
            $("#statsContainer")
                .empty()
                .append(statistics);

        })
        .fail(function(){
            showAlert("Ein Fehler ist beim abrufen von Daten aufgetreten!", "alert-danger");
        });
}