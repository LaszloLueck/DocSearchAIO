using DocSearchAIO.DocSearch.TOs;
using LanguageExt;

namespace DocSearchAIO.Endpoints.Administration.Statistics;

[Record]
public record IndexStatistic(IAsyncEnumerable<IndexStatisticModel> IndexStatisticModels,
    Seq<(string, RunnableStatistic)> RuntimeStatistics, long EntireDocCount, double EntireSizeInBytes);