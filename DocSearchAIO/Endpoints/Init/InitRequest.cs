using System.Text.Json.Serialization;

namespace DocSearchAIO.Endpoints.Init;

public class InitRequest
{
    [JsonPropertyName("filterExcel")] public bool FilterExcel { get; set; }

    [JsonPropertyName("filterWord")] public bool FilterWord { get; set; }

    [JsonPropertyName("filterPowerpoint")] public bool FilterPowerpoint { get; set; }

    [JsonPropertyName("filterPdf")] public bool FilterPdf { get; set; }

    [JsonPropertyName("filterMsg")] public bool FilterMsg { get; set; }

    [JsonPropertyName("filterEml")] public bool FilterEml { get; set; }

    [JsonPropertyName("itemsPerPage")] public int ItemsPerPage { get; set; }
}