using System.Diagnostics;
using DocSearchAIO.Classes;
using DocSearchAIO.Endpoints.Detail;
using DocSearchAIO.Services;
using Nest;
using SourceFilter = Nest.SourceFilter;

namespace DocSearchAIO.DocSearch.Services;

public interface IDocumentDetailService
{
    public Task<DocumentDetailModel> DocumentDetail(DocumentDetailRequest request);
}

public class DocumentDetailService : IDocumentDetailService
{
    private readonly ILogger _logger;
    private readonly IElasticSearchService _elasticSearchService;

    public DocumentDetailService(IElasticSearchService elasticSearchService, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DocumentDetailService>();
        _elasticSearchService = elasticSearchService;
    }

    public async Task<DocumentDetailModel> DocumentDetail(
        DocumentDetailRequest request)
    {
        try
        {
            var sw = Stopwatch.StartNew();

            static async Task<DocumentDetailModel> RequestAndConvert<T>(DocumentDetailRequest request,
                IElasticSearchService elasticSearchService) where T : ElasticDocument
            {
                var query = new TermQuery { Field = new Field("_id"), Value = request.Id };
                var exclude = new[] { new Field("completionContent"), new Field("content") };
                var sf = new SourceFilter { Excludes = exclude };
                var searchRequest = new SearchRequest(request.IndexName)
                {
                    Query = new QueryContainer(query),
                    Source = sf
                };

                var result = await elasticSearchService.SearchIndexAsync<T>(searchRequest);
                return result
                    .Hits
                    .ToOption()
                    .Match(
                        hit =>
                        {
                            var source = hit.Source switch
                            {
                                WordElasticDocument wordElasticDocument => new DocumentDetailModel(
                                    wordElasticDocument.Creator, wordElasticDocument.Created,
                                    wordElasticDocument.LastModifiedBy, wordElasticDocument.Modified,
                                    wordElasticDocument.Title, wordElasticDocument.Subject, wordElasticDocument.Version,
                                    wordElasticDocument.Revision, wordElasticDocument.LastPrinted,
                                    wordElasticDocument.UriFilePath[
                                        (wordElasticDocument.UriFilePath.LastIndexOf("/", StringComparison.Ordinal) +
                                         1)..],
                                    wordElasticDocument.ProcessTime),
                                ExcelElasticDocument excelElasticDocument => new DocumentDetailModel(
                                    excelElasticDocument.Creator, excelElasticDocument.Created,
                                    excelElasticDocument.LastModifiedBy, excelElasticDocument.Modified,
                                    excelElasticDocument.Title, excelElasticDocument.Subject,
                                    excelElasticDocument.Version, excelElasticDocument.Revision,
                                    excelElasticDocument.LastPrinted,
                                    excelElasticDocument.UriFilePath[
                                        (excelElasticDocument.UriFilePath.LastIndexOf("/", StringComparison.Ordinal) +
                                         1)..],
                                    excelElasticDocument.ProcessTime),
                                PowerpointElasticDocument powerpointElasticDocument => new DocumentDetailModel(
                                    powerpointElasticDocument.Creator, powerpointElasticDocument.Created,
                                    powerpointElasticDocument.LastModifiedBy, powerpointElasticDocument.Modified,
                                    powerpointElasticDocument.Title, powerpointElasticDocument.Subject,
                                    powerpointElasticDocument.Version, powerpointElasticDocument.Revision,
                                    powerpointElasticDocument.LastPrinted,
                                    powerpointElasticDocument.UriFilePath[
                                        (powerpointElasticDocument.UriFilePath.LastIndexOf("/",
                                            StringComparison.Ordinal) + 1)..], powerpointElasticDocument.ProcessTime),
                                PdfElasticDocument pdfElasticDocument => new DocumentDetailModel(
                                    pdfElasticDocument.Creator, new DateTime(1970, 01, 01, 0, 0, 0), string.Empty,
                                    new DateTime(1970, 01, 01, 0, 0, 0), pdfElasticDocument.Title,
                                    pdfElasticDocument.Subject, string.Empty, string.Empty,
                                    new DateTime(1970, 01, 01, 0, 0, 0), pdfElasticDocument.UriFilePath[
                                        (pdfElasticDocument.UriFilePath.LastIndexOf("/",
                                            StringComparison.Ordinal) + 1)..],
                                    pdfElasticDocument.ProcessTime),
                                EmlElasticDocument emlElasticDocument => new DocumentDetailModel(
                                    emlElasticDocument.Creator, new DateTime(1970, 01, 01, 0, 0, 0), string.Empty,
                                    new DateTime(1970, 01, 01, 0, 0, 0), emlElasticDocument.Title,
                                    emlElasticDocument.Subject, string.Empty, string.Empty,
                                    new DateTime(1970, 01, 01, 0, 0, 0), emlElasticDocument.UriFilePath[
                                        (emlElasticDocument.UriFilePath.LastIndexOf("/",
                                            StringComparison.Ordinal) + 1)..],
                                    emlElasticDocument.ProcessTime),
                                MsgElasticDocument msgElasticDocument => new DocumentDetailModel(
                                    msgElasticDocument.Creator, new DateTime(1970, 01, 01, 0, 0, 0), string.Empty,
                                    new DateTime(1970, 01, 01, 0, 0, 0), msgElasticDocument.Title,
                                    msgElasticDocument.Subject, string.Empty, string.Empty,
                                    new DateTime(1970, 01, 01, 0, 0, 0), msgElasticDocument.UriFilePath[
                                        (msgElasticDocument.UriFilePath.LastIndexOf("/",
                                            StringComparison.Ordinal) + 1)..],
                                    msgElasticDocument.ProcessTime),
                                _ => throw new ArgumentOutOfRangeException(hit.Source.GetType().Name, hit.Source, null)
                            };

                            source.Id = hit.Id;
                            return source;
                        },
                        () => new DocumentDetailModel(string.Empty, new DateTime(1970, 01, 01, 0, 0, 0), string.Empty,
                            new DateTime(1970, 01, 01, 0, 0, 0),
                            string.Empty, string.Empty, string.Empty, string.Empty, new DateTime(1970, 01, 01, 0, 0, 0),
                            string.Empty, new DateTime(1970, 01, 01, 0, 0, 0))
                    );
            }

            var result = request.IndexName switch
            {
                "officedocuments-word" => RequestAndConvert<WordElasticDocument>(request, _elasticSearchService),
                "officedocuments-excel" => RequestAndConvert<ExcelElasticDocument>(request, _elasticSearchService),
                "officedocuments-powerpoint" => RequestAndConvert<PowerpointElasticDocument>(request,
                    _elasticSearchService),
                "officedocuments-pdf" => RequestAndConvert<PdfElasticDocument>(request, _elasticSearchService),
                "officedocuments-eml" => RequestAndConvert<EmlElasticDocument>(request, _elasticSearchService),
                "officedocuments-msg" => RequestAndConvert<MsgElasticDocument>(request, _elasticSearchService),
                _ => throw new ArgumentOutOfRangeException(request.IndexName, request.IndexName, null)
            };

            var res = await result;
            sw.Stop();
            _logger.LogInformation("find document detail in {ElapsedTimeMs} ms", sw.ElapsedMilliseconds);
            return res;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An error occured");
            return new DocumentDetailModel(string.Empty, new DateTime(1970, 01, 01, 0, 0, 0), string.Empty,
                new DateTime(1970, 01, 01, 0, 0, 0),
                string.Empty, string.Empty, string.Empty, string.Empty, new DateTime(1970, 01, 01, 0, 0, 0),
                string.Empty, new DateTime(1970, 01, 01, 0, 0, 0));
        }
    }
}