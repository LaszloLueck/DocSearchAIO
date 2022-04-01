using DocSearchAIO.Classes;

namespace DocSearchAIO.DocSearch.TOs
{
    public record DocumentDetailModel(string Creator, DateTime Created, string LastModifiedBy, DateTime LastModified,
        string Title, string Subject, string Version, string Revision, DateTime LastPrinted, string FileName, DateTime LastProcessTime)
    {
        public string Id { get; set; } = string.Empty;

        public static implicit operator DocumentDetailModel(WordElasticDocument wordElasticDocument) =>
            new(wordElasticDocument.Creator, wordElasticDocument.Created,
                wordElasticDocument.LastModifiedBy, wordElasticDocument.Modified,
                wordElasticDocument.Title, wordElasticDocument.Subject, wordElasticDocument.Version,
                wordElasticDocument.Revision, wordElasticDocument.LastPrinted, 
                wordElasticDocument.UriFilePath[(wordElasticDocument.UriFilePath.LastIndexOf("/", StringComparison.Ordinal) +1)..],
                wordElasticDocument.ProcessTime);

    }
}