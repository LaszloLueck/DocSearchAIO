using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
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
        private readonly IElasticSearchService _elasticSearchService;

        public DocumentDetailService(IElasticSearchService elasticSearchService, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DocumentDetailService>();
            _elasticSearchService = elasticSearchService;
        }

        public async Task<DocumentDetailModel> GetDocumentDetail(
            DocumentDetailRequest request)
        {
            try
            {
                var query = new TermQuery {Field = new Field("_id"), Value = request.Id};
                var exclude = new[] {new Field("completionContent"), new Field("content")};
                var sf = new SourceFilter {Excludes = exclude};
                var searchRequest = new SearchRequest
                {
                    Query = new QueryContainer(query), Source = sf
                };
                var sw = Stopwatch.StartNew();
                var result = await _elasticSearchService.SearchIndexAsync<WordElasticDocument>(searchRequest);
                sw.Stop();
                _logger.LogInformation("find document detail in {ElapsedTimeMs} ms", sw.ElapsedMilliseconds);
                return result
                    .Hits
                    .TryFirst()
                    .Match(
                        Some: hit =>
                        {
                            var source = hit.Source;
                            return new DocumentDetailModel
                            {
                                Creator = source.Creator,
                                Created = source.Created.ToString("dd.MM.yyyy HH:mm:ss"),
                                Id = hit.Id,
                                LastModified = source.Modified.ToString("dd.MM.yyyy HH:mm:ss"),
                                LastModifiedBy = source.LastModifiedBy,
                                Revision = source.Revision,
                                Subject = source.Subject,
                                Title = source.Title,
                                Version = source.Version
                            };
                        },
                        None: () => new DocumentDetailModel()
                    );
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An error occured");
                return new DocumentDetailModel();
            }
        }
    }
}