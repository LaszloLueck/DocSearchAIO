doSearch = (searchText, from) => {
    const data = {
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
        url: "https://localhost:5001/api/search/doSearch",
        data: JSON.stringify(data)
    })
        .done(function (result) {
            $("#suggestedDocCount").text(result.docCount + " Dokumente");
            $("#searchTime").text("Suchzeit " + result.searchTime + " ms");
            $('#searchField').val(result.searchPhrase);
            if(result.docCount == 0){
                showAlert("Keine Dokumente mit dem Suchbegriff " + result.searchPhrase + " gefunden!", "alert-warning")
            }
            
            $("#searchResults").empty();
            var inner = result.searchResults.replaceAll("[#OO#]", "<b style='color:orange'>").replaceAll("[#CO#]", "</b>");

            $("#searchResults").html(inner);

            document.title = result.title;

            $("#pagination").empty();
            $("#pagination").append(result.pagination);

        })
        .fail(function(xhr, status, error){
            showAlert("Ein Fehler ist beim abrufen von Daten aufgetreten!", "alert-danger");
        });
}