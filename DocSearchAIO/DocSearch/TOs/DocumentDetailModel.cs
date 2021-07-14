using DocSearchAIO.Classes;

namespace DocSearchAIO.DocSearch.TOs
{
    public record DocumentDetailModel(string Creator, string Created, string LastModifiedBy, string LastModified,
        string Title, string Subject, string Version, string Revision, string LastPrinted)
    {
        public string Id { get; set; } = string.Empty;

        public static implicit operator DocumentDetailModel(WordElasticDocument wordElasticDocument) =>
            new(wordElasticDocument.Creator, wordElasticDocument.Created.ToString("dd.MM.yyyy HH:mm:ss"),
                wordElasticDocument.LastModifiedBy, wordElasticDocument.Modified.ToString("dd.MM.yyyy HH:mm:ss"),
                wordElasticDocument.Title, wordElasticDocument.Subject, wordElasticDocument.Version,
                wordElasticDocument.Revision, wordElasticDocument.LastPrinted.ToString("dd.MM.yyyy HH:mm:ss"));

    }
}