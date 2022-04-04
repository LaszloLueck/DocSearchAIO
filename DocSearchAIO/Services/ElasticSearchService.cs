using DocSearchAIO.Classes;
using DocSearchAIO.DocSearch.ServiceHooks;
using Elasticsearch.Net;
using Nest;

namespace DocSearchAIO.Services;

public interface IElasticSearchService
{
    Task<bool> BulkWriteDocumentsAsync<T>(IEnumerable<T> documents, string indexName)
        where T : ElasticDocument;

    Task<IndicesStatsResponse> IndexStatistics(string indexName);

    Task<bool> CreateIndexAsync<T>(string indexName) where T : ElasticDocument;
    Task<bool> DeleteIndexAsync(string indexName);
    Task<bool> RefreshIndexAsync(string indexName);
    Task<bool> FlushIndexAsync(string indexName);
    Task<GetIndexResponse> IndicesWithPatternAsync(string pattern, bool logToConsole = true);
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

    public async Task<IndicesStatsResponse> IndexStatistics(string indexName)
    {
        var indicesStatsResponse = await _elasticClient.Indices.StatsAsync(indexName);
        if (indicesStatsResponse.IsValid) return indicesStatsResponse;
        _logger.LogWarning("{DebugInfo}", indicesStatsResponse.DebugInformation);
        _logger.LogError(indicesStatsResponse.OriginalException,
            "{Reason}", indicesStatsResponse.ServerError.Error.Reason);
        return indicesStatsResponse;
    }

    public async Task<bool> RemoveItemById(string indexName, string id)
    {
        var deleteResponse = await _elasticClient.DeleteAsync(
            new DocumentPath<ElasticDocument>(new ElasticDocument {Id = id}),
            f => f.Index(indexName));
        if (deleteResponse.IsValid) return deleteResponse.IsValid;
        _logger.LogWarning("{DebugInfo}", deleteResponse.DebugInformation);
        _logger.LogError(deleteResponse.OriginalException, "{Reason}",
            deleteResponse.ServerError.Error.Reason);
        return deleteResponse.IsValid;
    }

    public async Task<int> RemoveItemsById(string indexName, IEnumerable<string> toRemove)
    {
        var bulkResponse =
            await _elasticClient.DeleteManyAsync(
                toRemove.Select(id => new ElasticDocument {Id = id}), indexName);
        if (bulkResponse.IsValid)
        {
            _logger.LogInformation(
                "{Info}", $"Successfully processed {bulkResponse.Items.Count} documents to elastic");
            return bulkResponse.Items.Count;
        }

        _logger.LogWarning("{DebugInfo}", bulkResponse.DebugInformation);
        _logger.LogError(bulkResponse.OriginalException, "{Reason}", bulkResponse.ServerError.Error.Reason);
        return bulkResponse.Items.Count;
    }

    public async Task<bool> BulkWriteDocumentsAsync<T>(IEnumerable<T> documents, string indexName)
        where T : ElasticDocument
    {
        var docs = documents.ToArray();
            
        if (!docs.Any())
            return await Task.Run(() => false);

        var bulkResponse = await _elasticClient.IndexManyAsync(docs, indexName);
        if (bulkResponse.IsValid)
        {
            _logger.LogInformation(
                "{Info}", $"Successfully processed {bulkResponse.Items.Count} documents to elastic");
            return bulkResponse.IsValid;
        }

        _logger.LogWarning("{DebugInfo}", bulkResponse.DebugInformation);
        _logger.LogError(bulkResponse.OriginalException, "{Reason}", bulkResponse.ServerError.Error.Reason);
        return bulkResponse.IsValid;
    }

    public async Task<bool> CreateIndexAsync<T>(string indexName) where T : ElasticDocument
    {
        var ci = new CreateIndexDescriptor(indexName).Map<ElasticDocument>(t => t.AutoMap());
        var mapString =
            _elasticClient.RequestResponseSerializer.SerializeToString(ci, SerializationFormatting.Indented);
        _logger.LogInformation("define mapping for index {IndexName}:", indexName);
        _logger.LogInformation("Mapping: {Mapping}", mapString);


        var createIndexResponse = await _elasticClient.Indices.CreateAsync(indexName, index => index.Map<T>(t => t.AutoMap()));

        if (createIndexResponse.IsValid) return createIndexResponse.IsValid;
        _logger.LogWarning("{DebugInfo}", createIndexResponse.DebugInformation);
        _logger.LogError(createIndexResponse.OriginalException,
            "{Reason}", createIndexResponse.ServerError.Error.Reason);
        return createIndexResponse.IsValid;
    }

    public async Task<bool> DeleteIndexAsync(string indexName)
    {
        var deleteIndexResponse = await _elasticClient.Indices.DeleteAsync(indexName);
        if (deleteIndexResponse.Acknowledged) return deleteIndexResponse.Acknowledged;
        _logger.LogWarning("{DebugInfo}", deleteIndexResponse.DebugInformation);
        _logger.LogError(deleteIndexResponse.OriginalException,
            "{Reason}", deleteIndexResponse.ServerError.Error.Reason);
        return deleteIndexResponse.Acknowledged;
    }

    public async Task<bool> RefreshIndexAsync(string indexName)
    {
        var refreshResponse = await _elasticClient.Indices.RefreshAsync(indexName);
        if (refreshResponse.IsValid) return refreshResponse.IsValid;
        _logger.LogWarning("{DebugInfo}", refreshResponse.DebugInformation);
        _logger.LogError(refreshResponse.OriginalException, "{Reason}",
            refreshResponse.ServerError.Error.Reason);
        return refreshResponse.IsValid;
    }

    public async Task<bool> FlushIndexAsync(string indexName)
    {
        var flushResponse = await _elasticClient.Indices.FlushAsync(indexName);
        if (flushResponse.IsValid) return flushResponse.IsValid;
        _logger.LogWarning("{DebugInfo}", flushResponse.DebugInformation);
        _logger.LogError(flushResponse.OriginalException, "{Reason}",
            flushResponse.ServerError.Error.Reason);
        return flushResponse.IsValid;
    }

    public async Task<GetIndexResponse> IndicesWithPatternAsync(string pattern, bool logToConsole = true)
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
            _logger.LogInformation("{Log}", log);
        }

        var getIndexResponse = await _elasticClient.Indices.GetAsync(getIndexRequest);
        if (getIndexResponse.IsValid) return getIndexResponse;
        _logger.LogWarning("{DebugInfo}", getIndexResponse.DebugInformation);
        _logger.LogError(getIndexResponse.OriginalException, "{Reason}",
            getIndexResponse.ServerError.Error.Reason);
        return getIndexResponse;
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
            _logger.LogInformation("{Log}", log);
        }

        var searchResponse = await _elasticClient.SearchAsync<T>(searchRequest);
        if (searchResponse.IsValid) return searchResponse;
        _logger.LogWarning("{DebugInfo}", searchResponse.DebugInformation);
        _logger.LogError(searchResponse.OriginalException, "{Reason}",
            searchResponse.ServerError.Error.Reason);
        return searchResponse;
    }
}