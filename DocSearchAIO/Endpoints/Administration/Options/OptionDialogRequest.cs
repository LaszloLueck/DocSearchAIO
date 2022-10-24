using System.Text.Json.Serialization;
using LanguageExt;

namespace DocSearchAIO.Endpoints.Administration.Options;

[Record]
public sealed record OptionDialogRequest(
    [property: JsonPropertyName("filterWord")]
    bool FilterWord,
    [property: JsonPropertyName("filterExcel")]
    bool FilterExcel,
    [property: JsonPropertyName("filterPowerpoint")]
    bool FilterPowerpoint,
    [property: JsonPropertyName("filterPdf")]
    bool FilterPdf,
    [property: JsonPropertyName("filterMsg")]
    bool FilterMsg,
    [property: JsonPropertyName("filterEml")]
    bool FilterEml,
    [property: JsonPropertyName("wordIndexExists")]
    bool WordIndexExists,
    [property: JsonPropertyName("excelIndexExists")]
    bool ExcelIndexExists,
    [property: JsonPropertyName("powerpointIndexExists")]
    bool PowerpointIndexExists,
    [property: JsonPropertyName("pdfIndexExists")]
    bool PdfIndexExists,
    [property: JsonPropertyName("msgIndexExists")]
    bool MsgIndexExists,
    [property: JsonPropertyName("emlIndexExists")]
    bool EmlIndexExists,
    [property: JsonPropertyName("itemsPerPage")]
    int ItemsPerPage);