using DocSearchAIO.Classes;

namespace DocSearchAIO.DocSearch.TOs
{
    public class DocumentDetailModel
    {
        public string Creator { get; set; } = string.Empty;
        public string Created { get; set; } = string.Empty;
        public string LastModifiedBy { get; set; } = string.Empty;
        public string LastModified { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Revision { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string LastPrinted { get; set; } = string.Empty;

        public static implicit operator DocumentDetailModel(WordElasticDocument wordElasticDocument) => new()
        {
            Created = wordElasticDocument.Created.ToString("dd.MM.yyyy HH:mm:ss"),
            Creator = wordElasticDocument.Creator,
            LastModified = wordElasticDocument.Modified.ToString("dd.MM.yyyy HH:mm:ss"),
            LastModifiedBy = wordElasticDocument.LastModifiedBy,
            Revision = wordElasticDocument.Revision,
            Subject = wordElasticDocument.Subject,
            Title = wordElasticDocument.Title,
            Version = wordElasticDocument.Version,
            LastPrinted = wordElasticDocument.LastPrinted.ToString("dd.MM.yyyy HH:mm:ss")
        };

    }
}