using System;
using Nest;

namespace DocSearchAIO.Classes
{
    public class ElasticDocument
    {
        [Text(Name = "id")]
        public string Id { get; set; }
        
        [Text(Name = "contentHash")]
        public string ContentHash { get; set; }
        
        [Text(Name = "originalFilePath")]
        public string OriginalFilePath { get; set; }
        
        [Text(Name = "uriFilePath")]
        public string UriFilePath { get; set; }

        [Text(Name = "content", TermVector = TermVectorOption.WithPositionsOffsetsPayloads)]
        public string Content { get; set; } [Completion(Name = "completionContent")]
        
        public CompletionField CompletionContent { get; set; }
        
        [Text(Name = "createdBy")] 
        public string Creator { get; set; }
        
        [Text(Name = "keywords")]
        public string[] Keywords { get; set; }
        
        [Text(Name = "subject")] 
        public string Subject { get; set; }
        
        [Date(Name = "processTime")] 
        public DateTime ProcessTime { get; set; }
        
        [Text(Name = "title")] 
        public string Title { get; set; }
        
        [Text(Name = "contentType")] 
        public string ContentType { get; set; }

    }
}