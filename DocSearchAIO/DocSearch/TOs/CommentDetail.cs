using System;
using System.Text.Json.Serialization;

namespace DocSearchAIO.DocSearch.TOs
{
    public record CommentDetail(
        //[JsonProperty("commentText", ItemConverterType = typeof(string))]
        //[JsonPropertyName("commentText")]
        string CommentText)
    {
        //[JsonProperty("author", ItemConverterType = typeof(string), NullValueHandling = NullValueHandling.Ignore)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("author")]
        public string? Author { get; set; }
        
        //[JsonProperty("date", ItemConverterType = typeof(DateTime), NullValueHandling = NullValueHandling.Ignore)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }
        
        //[JsonProperty("id", ItemConverterType = typeof(string), NullValueHandling = NullValueHandling.Ignore)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        
        //[JsonProperty("initials", ItemConverterType = typeof(string), NullValueHandling = NullValueHandling.Ignore)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("initials")]
        public string? Initials { get; set; }
        
    }
}