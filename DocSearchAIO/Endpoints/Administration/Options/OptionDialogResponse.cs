using System.Text.Json.Serialization;
using LanguageExt;

namespace DocSearchAIO.Endpoints.Administration.Options;

[Record]
public sealed record OptionDialogResponse(
    [property: JsonPropertyName("filterWord")] bool FilterWord, 
    [property: JsonPropertyName("filterExcel")] bool FilterExcel, 
    [property: JsonPropertyName("filterPowerpoint")] bool FilterPowerpoint, 
    [property: JsonPropertyName("filterPdf")] bool FilterPdf, 
    [property: JsonPropertyName("filterMsg")] bool FilterMsg, 
    [property: JsonPropertyName("filterEml")] bool FilterEml,
    [property: JsonPropertyName("itemsPerPage")] int ItemsPerPage)
{
    public bool WordIndexExists { get; set; }
    public bool ExcelIndexExists { get; set; }
    public bool PowerpointIndexExists { get; set; }
    public bool PdfIndexExists { get; set; }

    public bool MsgIndexExists { get; set; }

    public bool EmlIndexExists { get; set; }

    public static implicit operator OptionDialogResponse(OptionDialogRequest request) => new(
        request.FilterWord, request.FilterExcel, request.FilterPowerpoint, request.FilterPdf, request.FilterMsg, request.FilterEml,
        request.ItemsPerPage
    );
}