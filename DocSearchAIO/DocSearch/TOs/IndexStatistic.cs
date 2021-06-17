using System.Collections.Generic;

namespace DocSearchAIO.DocSearch.TOs
{
    public class IndexStatistic
    {
        public IEnumerable<IndexStatisticModel> IndexStatisticModels { get; set; }
        public Dictionary<string, RunnableStatistic> RuntimeStatistics { get; set; }
    }
}