using Newtonsoft.Json;

namespace DocSearchAIO.DocSearch.TOs
{
    public class InitRequestObject
    {
        [JsonProperty("filterExcel", ItemConverterType = typeof(bool), NullValueHandling = NullValueHandling.Ignore)]
        public bool FilterExcel { get; set; }
        [JsonProperty("filterWord", ItemConverterType = typeof(bool), NullValueHandling = NullValueHandling.Ignore)]
        public bool FilterWord { get; set; }
        [JsonProperty("filterPowerpoint", ItemConverterType = typeof(bool), NullValueHandling = NullValueHandling.Ignore)]
        public bool FilterPowerpoint { get; set; }
        [JsonProperty("filterPdf", ItemConverterType = typeof(bool), NullValueHandling = NullValueHandling.Ignore)]
        public bool FilterPdf { get; set; }
        [JsonProperty("itemsPerPage", ItemConverterType = typeof(int), NullValueHandling = NullValueHandling.Ignore)]
        public int? ItemsPerPage { get; set; }
    }
}