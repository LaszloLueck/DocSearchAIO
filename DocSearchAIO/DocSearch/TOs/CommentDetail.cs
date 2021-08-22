using System;
using Newtonsoft.Json;

namespace DocSearchAIO.DocSearch.TOs
{
    public record CommentDetail(
        [JsonProperty("commentText", ItemConverterType = typeof(string))]
        string CommentText)
    {
        [JsonProperty("author", ItemConverterType = typeof(string), NullValueHandling = NullValueHandling.Ignore)]
        public string? Author { get; set; }
        
        [JsonProperty("date", ItemConverterType = typeof(DateTime), NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? Date { get; set; }
        
        [JsonProperty("id", ItemConverterType = typeof(string), NullValueHandling = NullValueHandling.Ignore)]
        public string? Id { get; set; }
        
        [JsonProperty("initials", ItemConverterType = typeof(string), NullValueHandling = NullValueHandling.Ignore)]
        public string? Initials { get; set; }
        
    }
}