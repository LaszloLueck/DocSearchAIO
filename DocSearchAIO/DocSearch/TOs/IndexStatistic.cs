using System.Collections.Generic;
using DocumentFormat.OpenXml.Math;

namespace DocSearchAIO.DocSearch.TOs
{
    public class IndexStatistic
    {
        public IEnumerable<IndexStatisticModel> IndexStatisticModels { get; set; }
        public Dictionary<string, RunnableStatistic> RuntimeStatistics { get; set; }
    }
}