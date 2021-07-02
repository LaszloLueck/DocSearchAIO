doSearch = (searchText, from) => {
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
            $('#searchField').val(result.searchPhrase);
            if(result.docCount === 0){
                showAlert("Keine Dokumente mit dem Suchbegriff " + result.searchPhrase + " gefunden!", "alert-warning")
            }
            let searchResults = $('#searchResults');
            $(searchResults).empty();
            let inner = result.searchResults.replaceAll("[#OO#]", "<b style='color:orange'>").replaceAll("[#CO#]", "</b>");

            $(searchResults).html(inner);

            document.title = result.title;

            $("#pagination")
                .empty()
                .append(result.pagination);
            
            $("#statsContainer")
                .empty()
                .append(result.statistics);

        })
        .fail(function(){
            showAlert("Ein Fehler ist beim abrufen von Daten aufgetreten!", "alert-danger");
        });
}