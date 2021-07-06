using System;
using System.Collections.Generic;

namespace DocSearchAIO.DocSearch.TOs
{
    public class IndexStatistic
    {
        public IEnumerable<IndexStatisticModel> IndexStatisticModels { get; set; } = Array.Empty<IndexStatisticModel>();
        public Dictionary<string, RunnableStatistic> RuntimeStatistics { get; set; } = new();
    }
}