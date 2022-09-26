using System.Text.Json.Serialization;

namespace DocSearchAIO.DocSearch.TOs;

public sealed class CommentDetail
{
    public CommentDetail(string commentText)
    {
        CommentText = commentText;
    }

    [JsonPropertyName("commentText")] public string CommentText { get; } = null!;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("author")]
    public string Author { get; set; } = null!;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("initials")]
    public string Initials { get; set; } = null!;
}