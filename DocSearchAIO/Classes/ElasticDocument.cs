using System;
using Nest;

namespace DocSearchAIO.Classes
{
    public class ElasticDocument
    {
        [Text(Name = "id")]
        public string Id { get; set; } = string.Empty;
        
        [Text(Name = "contentHash")]
        public string ContentHash { get; set; } = string.Empty;

        [Text(Name = "originalFilePath")] public string OriginalFilePath { get; set; } = string.Empty;
        
        [Text(Name = "uriFilePath")]
        public string UriFilePath { get; set; } = string.Empty;

        [Text(Name = "content", TermVector = TermVectorOption.WithPositionsOffsetsPayloads)]
        public string Content { get; set; } = string.Empty;

        [Completion(Name = "completionContent")]
        public CompletionField CompletionContent { get; set; } = new();
        
        [Text(Name = "createdBy")] 
        public string Creator { get; set; } = string.Empty;

        [Text(Name = "keywords")] public string[] Keywords { get; set; } = Array.Empty<string>();
        
        [Text(Name = "subject")] 
        public string Subject { get; set; } = string.Empty;
        
        [Date(Name = "processTime")] 
        public DateTime ProcessTime { get; set; }
        
        [Text(Name = "title")] 
        public string Title { get; set; } = string.Empty;
        
        [Text(Name = "contentType")] 
        public string ContentType { get; set; } = string.Empty;

    }
}