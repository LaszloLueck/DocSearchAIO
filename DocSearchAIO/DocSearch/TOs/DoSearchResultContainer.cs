using DocSearchAIO.Classes;

namespace DocSearchAIO.DocSearch.TOs
{
    public record DoSearchResultContainer(string RelativeUrl, string Id, string AbsoluteUrl, string DocumentType)
    {
        public IEnumerable<ContentDetail> Contents { get; set; } = Array.Empty<ContentDetail>();
        public IEnumerable<CommentDetail> Comments { get; set; } = Array.Empty<CommentDetail>();
        public double Relevance { get; set; }
        public string ProgramIcon { get; set; } = string.Empty;

        public static implicit operator DoSearchResultContainer(ElasticDocument document) =>
            new(document.UriFilePath, document.Id, document.OriginalFilePath, document.ContentType);
    }
}