using System;
using System.Collections.Generic;
using Nest;

namespace DocSearchAIO.DocSearch.Objects
{
    public class ElasticDocument
    {
        [Text(Name = "id")] public string Id { get; set; }

        [Text(Name = "contentHash")] public string ContentHash { get; set; }

        [Text(Name = "category")] public string Category { get; set; }

        [Text(Name = "createdBy")] public string Creator { get; set; }

        [Text(Name = "description")] public string Description { get; set; }

        [Text(Name = "identifier")] public string Identifier { get; set; }

        [Text(Name = "keywords")] public IEnumerable<string> Keywords { get; set; }

        [Text(Name = "language")] public string Language { get; set; }

        [Text(Name = "revision")] public string Revision { get; set; }

        [Text(Name = "subject")] public string Subject { get; set; }

        [Text(Name = "title")] public string Title { get; set; }

        [Text(Name = "version")] public string Version { get; set; }

        [Text(Name = "contentStatus")] public string ContentStatus { get; set; }

        [Text(Name = "contentType")] public string ContentType { get; set; }

        [Text(Name = "lastModifiedBy")] public string LastModifiedBy { get; set; }

        [Text(Name = "content")] public string Content { get; set; }

        [Text(Name = "originalFilePath")] public string OriginalFilePath { get; set; }

        [Text(Name = "uriFilePath")] public string UriFilePath { get; set; }

        [Completion(Name = "completionContent")]
        public CompletionField CompletionContent { get; set; }

        [Date(Name = "processTime")] public DateTime ProcessTime { get; set; }

        [Date(Name = "created")] public DateTime Created { get; set; }

        [Date(Name = "lastModified")] public DateTime Modified { get; set; }

        [Date(Name = "lastPrinted")] public DateTime LastPrinted { get; set; }
    }
}