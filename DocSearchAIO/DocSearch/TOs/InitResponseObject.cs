using System.Text.Json.Serialization;

namespace DocSearchAIO.DocSearch.TOs
{
    public class InitResponseObject
    {
       
        [JsonPropertyName("filterExcel")]
        public bool FilterExcel { get; set; }

        [JsonPropertyName("filterWord")]
        public bool FilterWord { get; set; }

        [JsonPropertyName("filterPowerpoint")]
        public bool FilterPowerpoint { get; set; }

        [JsonPropertyName("filterPdf")]
        public bool FilterPdf { get; set; }

        [JsonPropertyName("filterMsg")]
        public bool FilterMsg { get; set; }

        [JsonPropertyName("filterEml")]
        public bool FilterEml { get; set; }

        [JsonPropertyName("itemsPerPage")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ItemsPerPage { get; set; }

        [JsonPropertyName("wordFilterActive")]
        public bool WordFilterActive { get; set; }

        [JsonPropertyName("excelFilterActive")]
        public bool ExcelFilterActive { get; set; }

        [JsonPropertyName("powerpointFilterActive")]
        public bool PowerpointFilterActive { get; set; }

        [JsonPropertyName("pdfFilterActive")]
        public bool PdfFilterActive { get; set; }

        [JsonPropertyName("msgFilterActive")]
        public bool MsgFilterActive { get; set; }

        [JsonPropertyName("emlFilterActive")]
        public bool EmlFilterActive { get; set; }
    }
}