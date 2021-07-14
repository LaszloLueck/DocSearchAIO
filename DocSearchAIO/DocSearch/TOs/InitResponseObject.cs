using Newtonsoft.Json;

namespace DocSearchAIO.DocSearch.TOs
{
    public record InitResponseObject(
        [JsonProperty("filterExcel", ItemConverterType = typeof(bool))]
        bool FilterExcel,
        [JsonProperty("filterWord", ItemConverterType = typeof(bool))]
        bool FilterWord,
        [JsonProperty("filterPowerpoint", ItemConverterType = typeof(bool))]
        bool FilterPowerpoint,
        [JsonProperty("filterPdf", ItemConverterType = typeof(bool))]
        bool FilterPdf,
        [JsonProperty("itemsPerPage", ItemConverterType = typeof(int))]
        int ItemsPerPage,
        [JsonProperty("wordFilterActive", ItemConverterType = typeof(bool))]
        bool WordFilterActive,
        [JsonProperty("excelFilterActive", ItemConverterType = typeof(bool))]
        bool ExcelFilterActive,
        [JsonProperty("powerpointFilterActive", ItemConverterType = typeof(bool))]
        bool PowerpointFilterActive,
        [JsonProperty("pdfFilterActive", ItemConverterType = typeof(bool))]
        bool PdfFilterActive
    );

    // public class InitResponseObject
    // {
    //     [JsonProperty("filterExcel", ItemConverterType = typeof(bool))]
    //     public bool FilterExcel { get; set; }
    //     [JsonProperty("filterWord", ItemConverterType = typeof(bool))]
    //     public bool FilterWord { get; set; }
    //     [JsonProperty("filterPowerpoint", ItemConverterType = typeof(bool))]
    //     public bool FilterPowerpoint { get; set; }
    //     [JsonProperty("filterPdf", ItemConverterType = typeof(bool))]
    //     public bool FilterPdf { get; set; }
    //     [JsonProperty("itemsPerPage", ItemConverterType = typeof(int))]
    //     public int ItemsPerPage { get; set; }
    //     [JsonProperty("wordFilterActive", ItemConverterType = typeof(bool))]
    //      public bool WordFilterActive { get; set; }
    //      [JsonProperty("excelFilterActive", ItemConverterType = typeof(bool))]
    //      public bool ExcelFilterActive { get; set; }
    //      [JsonProperty("powerpointFilterActive", ItemConverterType = typeof(bool))]
    //      public bool PowerpointFilterActive { get; set; }
    //      [JsonProperty("pdfFilterActive", ItemConverterType = typeof(bool))]
    //      public bool PdfFilterActive { get; set; }     
    // }
}