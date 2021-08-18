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
        [JsonProperty("filterMsg", ItemConverterType = typeof(bool))]
        bool FilterMsg,
        [JsonProperty("filterEml", ItemConverterType = typeof(bool))]
        bool FilterEml,
        [JsonProperty("itemsPerPage", ItemConverterType = typeof(int))]
        int ItemsPerPage,
        [JsonProperty("wordFilterActive", ItemConverterType = typeof(bool))]
        bool WordFilterActive,
        [JsonProperty("excelFilterActive", ItemConverterType = typeof(bool))]
        bool ExcelFilterActive,
        [JsonProperty("powerpointFilterActive", ItemConverterType = typeof(bool))]
        bool PowerpointFilterActive,
        [JsonProperty("pdfFilterActive", ItemConverterType = typeof(bool))]
        bool PdfFilterActive,
        [JsonProperty("msgFilterActive", ItemConverterType = typeof(bool))]
        bool MsgFilterActive,
        [JsonProperty("emlFilterActive", ItemConverterType = typeof(bool))]
        bool EmlFilterActive
    );
}