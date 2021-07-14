using Newtonsoft.Json;

namespace DocSearchAIO.DocSearch.TOs
{
    public record InitRequest(
        [JsonProperty("filterExcel", ItemConverterType = typeof(bool), NullValueHandling = NullValueHandling.Ignore)]
        bool FilterExcel,
        [JsonProperty("filterWord", ItemConverterType = typeof(bool), NullValueHandling = NullValueHandling.Ignore)]
        bool FilterWord,
        [JsonProperty("filterPowerpoint", ItemConverterType = typeof(bool),
            NullValueHandling = NullValueHandling.Ignore)]
        bool FilterPowerpoint,
        [JsonProperty("filterPdf", ItemConverterType = typeof(bool), NullValueHandling = NullValueHandling.Ignore)]
        bool FilterPdf,
        [JsonProperty("itemsPerPage", ItemConverterType = typeof(int), NullValueHandling = NullValueHandling.Ignore)]
        int ItemsPerPage = 20
    );
}