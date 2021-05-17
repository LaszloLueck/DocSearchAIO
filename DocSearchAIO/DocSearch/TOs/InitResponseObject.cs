using Newtonsoft.Json;

namespace DocSearchAIO.DocSearch.TOs
{
    public class InitResponseObject
    {
        [JsonProperty("filterExcel", ItemConverterType = typeof(bool))]
        public bool FilterExcel { get; set; }
        [JsonProperty("filterWord", ItemConverterType = typeof(bool))]
        public bool FilterWord { get; set; }
        [JsonProperty("filterPowerpoint", ItemConverterType = typeof(bool))]
        public bool FilterPowerpoint { get; set; }
        [JsonProperty("filterPdf", ItemConverterType = typeof(bool))]
        public bool FilterPdf { get; set; }
        [JsonProperty("itemsPerPage", ItemConverterType = typeof(int))]
        public int ItemsPerPage { get; set; }
        [JsonProperty("wordFilterActive", ItemConverterType = typeof(bool))]
         public bool WordFilterActive { get; set; }
         [JsonProperty("excelFilterActive", ItemConverterType = typeof(bool))]
         public bool ExcelFilterActive { get; set; }
         [JsonProperty("powerpointFilterActive", ItemConverterType = typeof(bool))]
         public bool PowerpointFilterActive { get; set; }
         [JsonProperty("pdfFilterActive", ItemConverterType = typeof(bool))]
         public bool PdfFilterActive { get; set; }     
    }
}