namespace DocSearchAIO.DocSearch.TOs
{
    public record IndexStatistic(IAsyncEnumerable<IndexStatisticModel> IndexStatisticModels,
        Dictionary<string, RunnableStatistic> RuntimeStatistics, long EntireDocCount, double EntireSizeInBytes);
}