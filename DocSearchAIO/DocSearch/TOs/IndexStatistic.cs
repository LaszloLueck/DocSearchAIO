using System;
using System.Collections.Generic;

namespace DocSearchAIO.DocSearch.TOs
{
    public record IndexStatistic(IEnumerable<IndexStatisticModel> IndexStatisticModels,
        Dictionary<string, RunnableStatistic> RuntimeStatistics);


    // public class IndexStatistic
    // {
    //     public IEnumerable<IndexStatisticModel> IndexStatisticModels { get; set; } = Array.Empty<IndexStatisticModel>();
    //     public Dictionary<string, RunnableStatistic> RuntimeStatistics { get; set; } = new();
    // }
}