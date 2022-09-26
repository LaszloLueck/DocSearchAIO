using System.Text.Json.Serialization;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Utilities;

namespace DocSearchAIO.Endpoints.Init;

public sealed class InitResponseObject
{
    public InitResponseObject(){}
    
    public InitResponseObject(ConfigurationObject cfg, string[] indexNames, InitRequest initRequest)
    {
        FilterExcel =
            StaticHelpers.IndexKeyExpression<ExcelElasticDocument>(cfg, indexNames, initRequest.FilterExcel);
        FilterWord =
            StaticHelpers.IndexKeyExpression<WordElasticDocument>(cfg, indexNames, initRequest.FilterWord);
        FilterPowerpoint =
            StaticHelpers.IndexKeyExpression<PowerpointElasticDocument>(cfg, indexNames,
                initRequest.FilterPowerpoint);
        FilterPdf = StaticHelpers.IndexKeyExpression<PdfElasticDocument>(cfg, indexNames, initRequest.FilterPdf);
        FilterMsg = StaticHelpers.IndexKeyExpression<MsgElasticDocument>(cfg, indexNames, initRequest.FilterMsg);
        FilterEml = StaticHelpers.IndexKeyExpression<EmlElasticDocument>(cfg, indexNames, initRequest.FilterEml);
        ItemsPerPage = initRequest.ItemsPerPage;
        WordFilterActive = StaticHelpers.IndexKeyExpression<WordElasticDocument>(cfg, indexNames);
        ExcelFilterActive = StaticHelpers.IndexKeyExpression<ExcelElasticDocument>(cfg, indexNames);
        PowerpointFilterActive = StaticHelpers.IndexKeyExpression<PowerpointElasticDocument>(cfg, indexNames);
        PdfFilterActive = StaticHelpers.IndexKeyExpression<PdfElasticDocument>(cfg, indexNames);
        MsgFilterActive = StaticHelpers.IndexKeyExpression<MsgElasticDocument>(cfg, indexNames);
        EmlFilterActive = StaticHelpers.IndexKeyExpression<EmlElasticDocument>(cfg, indexNames);
    }

    [JsonPropertyName("filterExcel")] public bool FilterExcel { get; set; }

    [JsonPropertyName("filterWord")] public bool FilterWord { get; set; }

    [JsonPropertyName("filterPowerpoint")] public bool FilterPowerpoint { get; set; }

    [JsonPropertyName("filterPdf")] public bool FilterPdf { get; set; }

    [JsonPropertyName("filterMsg")] public bool FilterMsg { get; set; }

    [JsonPropertyName("filterEml")] public bool FilterEml { get; set; }

    [JsonPropertyName("itemsPerPage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ItemsPerPage { get; set; }

    [JsonPropertyName("wordFilterActive")] public bool WordFilterActive { get; set; }

    [JsonPropertyName("excelFilterActive")]
    public bool ExcelFilterActive { get; set; }

    [JsonPropertyName("powerpointFilterActive")]
    public bool PowerpointFilterActive { get; set; }

    [JsonPropertyName("pdfFilterActive")] public bool PdfFilterActive { get; set; }

    [JsonPropertyName("msgFilterActive")] public bool MsgFilterActive { get; set; }

    [JsonPropertyName("emlFilterActive")] public bool EmlFilterActive { get; set; }
}