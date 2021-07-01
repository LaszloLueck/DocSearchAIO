using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocSearchAIO.Classes;
using DocSearchAIO.DocSearch.ServiceHooks;
using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Nest;

namespace DocSearchAIO.Services
{
    public interface IElasticSearchService
    {
        Task<bool> BulkWriteDocumentsAsync<T>(IEnumerable<T> documents, string indexName)
            where T : ElasticDocument;

        Task<IndicesStatsResponse> GetIndexStatistics(string indexName);

        Task<bool> CreateIndexAsync<T>(string indexName) where T : ElasticDocument;
        Task<bool> DeleteIndexAsync(string indexName);
        Task<bool> RefreshIndexAsync(string indexName);
        Task<bool> FlushIndexAsync(string indexName);
        Task<GetIndexResponse> GetIndicesWithPatternAsync(string pattern, bool logToConsole = true);
        Task<bool> RemoveItemById(string indexName, string id);

        Task<int> RemoveItemsById(string indexName, IEnumerable<string> toRemove);
        Task<bool> IndexExistsAsync(string indexName);

        Task<ISearchResponse<T>> SearchIndexAsync<T>(SearchRequest searchRequest, bool logToConsole = true)
            where T : ElasticDocument;
    }

    public class ElasticSearchService : IElasticSearchService
    {
        private readonly ILogger _logger;
        private readonly IElasticClient _elasticClient;

        public ElasticSearchService(IElasticClient elasticClient)
        {
            _logger = LoggingFactoryBuilder.Build<ElasticSearchService>();
            _elasticClient = elasticClient;
        }

        public async Task<IndicesStatsResponse> GetIndexStatistics(string indexName)
        {
            var result = await _elasticClient.Indices.StatsAsync(indexName);
            ProcessResponse(result);
            return result;
        }

        public async Task<bool> RemoveItemById(string indexName, string id)
        {
            var result = await _elasticClient.DeleteAsync<ElasticDocument>(new DocumentPath<ElasticDocument>(new ElasticDocument { Id = id }),
                f => f.Index(indexName));
            return ProcessResponse(result);
        }

        public async Task<int> RemoveItemsById(string indexName, IEnumerable<string> toRemove)
        {
            var result = await _elasticClient.DeleteManyAsync<ElasticDocument>(toRemove.Select(id => new ElasticDocument { Id = id }), indexName);
            ProcessResponse(result);
            return result.Items.Count;
        }

        public async Task<bool> BulkWriteDocumentsAsync<T>(IEnumerable<T> documents, string indexName)
            where T : ElasticDocument
        {
            if (!documents.Any())
                return await Task.Run(() => false);

            var result = await _elasticClient.IndexManyAsync(documents, indexName);
            return ProcessResponse(result);
        }

        public async Task<bool> CreateIndexAsync<T>(string indexName) where T : ElasticDocument
        {
            var ci = new CreateIndexDescriptor(indexName).Map<ElasticDocument>(t => t.AutoMap());
            var mapString =
                _elasticClient.RequestResponseSerializer.SerializeToString(ci, SerializationFormatting.Indented);
            _logger.LogInformation("define mapping for index {IndexName}:", indexName);
            _logger.LogInformation("Mapping: {Mapping}", mapString);


            var result = await _elasticClient.Indices.CreateAsync(indexName, index => index.Map<T>(t => t.AutoMap()));

            return ProcessResponse(result);
        }

        public async Task<bool> DeleteIndexAsync(string indexName)
        {
            var result = await _elasticClient.Indices.DeleteAsync(indexName);
            return ProcessResponse(result);
        }

        public async Task<bool> RefreshIndexAsync(string indexName)
        {
            var result = await _elasticClient.Indices.RefreshAsync(indexName);
            return ProcessResponse(result);
        }

        public async Task<bool> FlushIndexAsync(string indexName)
        {
            var result = await _elasticClient.Indices.FlushAsync(indexName);
            return ProcessResponse(result);
        }

        public async Task<GetIndexResponse> GetIndicesWithPatternAsync(string pattern, bool logToConsole = true)
        {
            var getIndexRequest = pattern switch
            {
                "" => new GetIndexRequest(Indices.All),
                _ => new GetIndexRequest(pattern)
            };
            if (logToConsole)
            {
                var log = _elasticClient.RequestResponseSerializer.SerializeToString(getIndexRequest,
                    SerializationFormatting.Indented);
                _logger.LogInformation(log);
            }

            var result = await _elasticClient.Indices.GetAsync(getIndexRequest);
            ProcessResponse(result);
            return result;
        }

        public async Task<bool> IndexExistsAsync(string indexName)
        {
            var result = await _elasticClient.Indices.ExistsAsync(indexName);
            return result.Exists;
        }

        public async Task<ISearchResponse<T>> SearchIndexAsync<T>(SearchRequest searchRequest, bool logToConsole = true)
            where T : ElasticDocument
        {
            if (logToConsole)
            {
                var log = _elasticClient.RequestResponseSerializer.SerializeToString(searchRequest,
                    SerializationFormatting.Indented);
                _logger.LogInformation(log);
            }

            var searchResponse = await _elasticClient.SearchAsync<T>(searchRequest);
            ProcessResponse(searchResponse);
            return searchResponse;
        }

        private bool ProcessResponse<T>(T response)
        {
            switch (response)
            {
                case DeleteResponse deleteResponse:
                    if (deleteResponse.IsValid) return deleteResponse.IsValid;
                    _logger.LogWarning(deleteResponse.DebugInformation);
                    _logger.LogError(deleteResponse.OriginalException, deleteResponse.ServerError.Error.Reason);
                    return deleteResponse.IsValid;
                case IndicesStatsResponse indicesStatsResponse:
                    if (indicesStatsResponse.IsValid) return indicesStatsResponse.IsValid;
                    _logger.LogWarning(indicesStatsResponse.DebugInformation);
                    _logger.LogError(indicesStatsResponse.OriginalException,
                        indicesStatsResponse.ServerError.Error.Reason);
                    return indicesStatsResponse.IsValid;
                case ISearchResponse<WordElasticDocument> searchResponse:
                    if (searchResponse.IsValid) return searchResponse.IsValid;
                    _logger.LogWarning(searchResponse.DebugInformation);
                    _logger.LogError(searchResponse.OriginalException, searchResponse.ServerError.Error.Reason);
                    return searchResponse.IsValid;
                case GetIndexResponse getIndexResponse:
                    if (getIndexResponse.IsValid) return getIndexResponse.IsValid;
                    _logger.LogWarning(getIndexResponse.DebugInformation);
                    _logger.LogError(getIndexResponse.OriginalException, getIndexResponse.ServerError.Error.Reason);
                    return getIndexResponse.IsValid;
                case DeleteIndexResponse deleteIndexResponse:
                    if (deleteIndexResponse.Acknowledged) return deleteIndexResponse.Acknowledged;
                    _logger.LogWarning(deleteIndexResponse.DebugInformation);
                    _logger.LogError(deleteIndexResponse.OriginalException,
                        deleteIndexResponse.ServerError.Error.Reason);
                    return deleteIndexResponse.Acknowledged;
                case CreateIndexResponse createIndexResponse:
                    if (createIndexResponse.IsValid) return createIndexResponse.IsValid;
                    _logger.LogWarning(createIndexResponse.DebugInformation);
                    _logger.LogError(createIndexResponse.OriginalException,
                        createIndexResponse.ServerError.Error.Reason);
                    return createIndexResponse.IsValid;
                case FlushResponse flushResponse:
                    if (flushResponse.IsValid) return flushResponse.IsValid;
                    _logger.LogWarning(flushResponse.DebugInformation);
                    _logger.LogError(flushResponse.OriginalException, flushResponse.ServerError.Error.Reason);
                    return flushResponse.IsValid;
                case RefreshResponse refreshResponse:
                    if (refreshResponse.IsValid) return refreshResponse.IsValid;
                    _logger.LogWarning(refreshResponse.DebugInformation);
                    _logger.LogError(refreshResponse.OriginalException, refreshResponse.ServerError.Error.Reason);
                    return refreshResponse.IsValid;
                case BulkResponse bulkResponse:
                    if (bulkResponse.IsValid)
                    {
                        _logger.LogInformation(
                            $"Successfully processed {bulkResponse.Items.Count} documents to elastic");
                        return bulkResponse.IsValid;
                    }

                    _logger.LogWarning(bulkResponse.DebugInformation);
                    _logger.LogError(bulkResponse.OriginalException, bulkResponse.ServerError.Error.Reason);
                    return bulkResponse.IsValid;
                case IndexResponse indexResponse:
                    if (indexResponse.IsValid) return indexResponse.IsValid;
                    _logger.LogWarning(indexResponse.DebugInformation);
                    _logger.LogError(indexResponse.OriginalException, indexResponse.ServerError.Error.Reason);
                    return indexResponse.IsValid;
                case ExistsResponse existsResponse:
                    if (!existsResponse.Exists || existsResponse.IsValid) return existsResponse.Exists;
                    _logger.LogWarning(existsResponse.DebugInformation);
                    _logger.LogError(existsResponse.OriginalException, existsResponse.ServerError.Error.Reason);
                    return existsResponse.IsValid;
                case SearchResponse<ElasticDocument> searchResponse:
                    if (searchResponse.IsValid) return searchResponse.IsValid;
                    _logger.LogWarning(searchResponse.DebugInformation);
                    _logger.LogError(searchResponse.OriginalException, searchResponse.ServerError.Error.Reason);
                    return searchResponse.IsValid;
                default:
                    _logger.LogWarning($"Cannot find Conversion for type <{response.GetType().Name}>");
                    return false;
            }
        }
    }
}