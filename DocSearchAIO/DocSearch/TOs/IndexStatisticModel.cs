using System.Linq;
using Nest;

namespace DocSearchAIO.DocSearch.TOs
{
    public class IndexStatisticModel
    {
        public string IndexName { get; set; } = string.Empty;
        public long DocCount { get; set; }
        public double SizeInBytes { get; set; }
        
        public long FetchTimeMs { get; set; }
        
        public long FetchTotal { get; set; }
        public long QueryTimeMs { get; set; }
        public long QueryTotal { get; set; }
        public long SuggestTimeMs { get; set; }
        public long SuggestTotal { get; set; }

        public static explicit operator IndexStatisticModel(IndicesStatsResponse response) => new()
        {
            DocCount = response.Stats.Total.Documents.Count,
            FetchTimeMs = response.Stats.Total.Search.FetchTimeInMilliseconds,
            FetchTotal = response.Stats.Total.Search.FetchTotal,
            IndexName = response.Indices.First().Key,
            QueryTimeMs = response.Stats.Total.Search.QueryTimeInMilliseconds,
            QueryTotal = response.Stats.Total.Search.QueryTotal,
            SizeInBytes = response.Stats.Total.Store.SizeInBytes,
            SuggestTimeMs = response.Stats.Total.Search.SuggestTimeInMilliseconds,
            SuggestTotal = response.Stats.Total.Search.SuggestTotal
        };

    }
}