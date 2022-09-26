using System.Text.Json.Serialization;
using LanguageExt;

namespace DocSearchAIO.Endpoints.Search;


[Record]
public sealed record DoSearchRequest(
    [property:JsonPropertyName("searchPhrase")] string SearchPhrase, 
    [property:JsonPropertyName("from")] int From, 
    [property:JsonPropertyName("size")] int Size, 
    [property:JsonPropertyName("filterWord")] bool FilterWord, 
    [property:JsonPropertyName("filterExcel")] bool FilterExcel,
    [property:JsonPropertyName("filterPowerpoint")] bool FilterPowerpoint, 
    [property:JsonPropertyName("filterPdf")] bool FilterPdf, 
    [property:JsonPropertyName("filterMsg")] bool FilterMsg, 
    [property:JsonPropertyName("filterEml")] bool FilterEml);