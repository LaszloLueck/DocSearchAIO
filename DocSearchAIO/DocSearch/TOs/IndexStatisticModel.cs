using Nest;

namespace DocSearchAIO.DocSearch.TOs
{
    public record IndexStatisticModel(string IndexName, long DocCount, double SizeInBytes, long FetchTimeMs,
        long FetchTotal, long QueryTimeMs, long QueryTotal, long SuggestTimeMs, long SuggestTotal)
    {
        public static explicit operator IndexStatisticModel(IndicesStatsResponse response) =>
            new(response.Indices.First().Key, response.Stats.Total.Documents.Count, response.Stats.Total.Store
                    .SizeInBytes,
                response.Stats.Total.Search.FetchTimeInMilliseconds, response.Stats.Total.Search.FetchTotal, response
                    .Stats.Total.Search.QueryTimeInMilliseconds, response.Stats.Total.Search.QueryTotal,
                response.Stats.Total.Search.SuggestTimeInMilliseconds, response.Stats.Total.Search.SuggestTotal);
    }
}