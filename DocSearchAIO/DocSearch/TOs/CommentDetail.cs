using System.Text.Json.Serialization;

namespace DocSearchAIO.DocSearch.TOs
{
    public class CommentDetail
    {
        public CommentDetail(string commentText)
        {
            CommentText = commentText;
        }
        
        [JsonPropertyName("commentText")]
        public string CommentText { get; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("author")]
        public string? Author { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("initials")]
        public string? Initials { get; set; }
        
    }
}