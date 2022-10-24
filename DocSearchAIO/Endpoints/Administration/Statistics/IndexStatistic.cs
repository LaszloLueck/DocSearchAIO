using DocSearchAIO.DocSearch.TOs;
using LanguageExt;

namespace DocSearchAIO.Endpoints.Administration.Statistics;

[Record]
public sealed record IndexStatistic(IEnumerable<IndexStatisticModel> IndexStatisticModels,
    Dictionary<string, RunnableStatistic> RuntimeStatistics, long EntireDocCount, double EntireSizeInBytes);