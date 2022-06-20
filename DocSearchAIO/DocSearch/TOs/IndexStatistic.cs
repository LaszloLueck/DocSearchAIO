namespace DocSearchAIO.DocSearch.TOs;

public record IndexStatistic(IAsyncEnumerable<IndexStatisticModel> IndexStatisticModels,
    IEnumerable<KeyValuePair<string, RunnableStatistic>> RuntimeStatistics, long EntireDocCount, double EntireSizeInBytes);