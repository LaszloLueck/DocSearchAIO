using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DocSearchAIO.Classes;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Services;
using Microsoft.Extensions.Logging;
using Nest;
using SourceFilter = Nest.SourceFilter;

namespace DocSearchAIO.DocSearch.Services
{
    public class DocumentDetailService
    {
        private readonly ILogger _logger;
        private readonly ViewToStringRenderer _viewToStringRenderer;
        private readonly IElasticSearchService _elasticSearchService;

        public DocumentDetailService(IElasticSearchService elasticSearchService, ILoggerFactory loggerFactory, ViewToStringRenderer viewToStringRenderer)
        {
            _logger = loggerFactory.CreateLogger<DocumentDetailService>();
            _viewToStringRenderer = viewToStringRenderer;
            _elasticSearchService = elasticSearchService;
        }

        public async Task<DocumentDetailResponse> GetDocumentDetail(
            DocumentDetailRequest request)
        {
            try
            {
                var query = new TermQuery();
                query.Field = new Field("_id");
                query.Value = request.Id;
                Field[] exclude = new[] {new Field("completionContent"), new Field("content")};
                var sf = new SourceFilter {Excludes = exclude};
                var searchRequest = new SearchRequest
                {
                    Query = new QueryContainer(query), Source = sf
                };
                var sw = Stopwatch.StartNew();
                var result = await _elasticSearchService.SearchIndexAsync<WordElasticDocument>(searchRequest);
                sw.Stop();
                _logger.LogInformation($"find documentdetail in {sw.ElapsedMilliseconds} ms");

                var returnObject = new DocumentDetailModel();
                if (result.Total >= 1)
                {
                    var hit = result.Hits.First();
                    returnObject.Creator = hit.Source.Creator;
                    returnObject.Created = hit.Source.Created.ToString("dd.MM.yyyy HH:mm:ss");
                    returnObject.LastModified = hit.Source.Modified.ToString("dd.MM.yyyy HH:mm:ss");
                    returnObject.LastModifiedBy = hit.Source.LastModifiedBy;
                    returnObject.Title = hit.Source.Title;
                    returnObject.Subject = hit.Source.Subject;
                    returnObject.Revision = hit.Source.Revision;
                    returnObject.Version = hit.Source.Version;
                    returnObject.Id = hit.Id;
                }

                var partialContent = await _viewToStringRenderer.Render("DocumentDetailModalPartial", returnObject);


                return new DocumentDetailResponse()
                    {Content = partialContent, State = "OK", ElementName = "#documentDetailModal"};
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An error occured");
                return new DocumentDetailResponse() {State = "ERROR"};
            }
        }
    }
}