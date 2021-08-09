renderSearchResultDetails = (detailElements) => {
    let mainContent = '';
    detailElements.forEach(element => {
        mainContent += '<div class="w-100 mb-2">';
        mainContent += '<h4 class="mb-0">';
        mainContent += '<img class="mr-1" src="' + element.programIcon + '" height="35" width="35" alt="ProgramIcon" />';
        mainContent += '<img class="mr-1" style="cursor:pointer" src="images/info.svg" alt="info" height="32" width="32" id="info_' + element.id + '" onclick="openDocumentDetailDialog(\'' + element.id + '\')" />';
        mainContent += '</h4>';
        mainContent += '</div>';
    });
    return mainContent;
}

/*
@foreach (var item in Model)
{
    <div class="w-100 mb-2">
        <h4 class="mb-0">
            <img class="mr-1" src="@item.ProgramIcon" height="35" width="35" alt="ProgramIcon"/>
            <img class="mr-1" style="cursor: pointer" src="images/info.svg" alt="info" height="32" width="32" id="info_@item.Id" onclick="openDocumentDetailDialog(@Html.Raw("'" + item.Id + "'"))"/>
            <a href="@item.RelativeUrl">
                <small>@item.RelativeUrl</small>
            </a>
        </h4>
        <h5>
            <small>Download</small>
            <a href="/api/base/download?path=@item.AbsoluteUrl&documentType=@item.DocumentType" target="_blank">
                <small>@item.AbsoluteUrl</small>
            </a>
        </h5>
        @foreach (var outer in item.SearchBody)
        {
            <div class="mb-1">
                <div>
                    <small class="text-muted">@outer.ContentType: </small>
                </div>
                <ul class="list-group">

                    @foreach (var innerItem in outer.ContentValues)
                    {
                        <li class="list-group-item py-2">
                            <small>@Html.Raw(innerItem)</small>
                        </li>
                    }
                </ul>
            </div>
        }
        <h6>
            <small class="text-muted">Relevanz: @item.Relevance | Id: @item.Id</small>
        </h6>
    </div>
}

 */