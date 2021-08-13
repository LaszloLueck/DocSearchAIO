﻿using System;
using System.Collections.Generic;

namespace DocSearchAIO.DocSearch.TOs
{
    public record IndexStatistic(IEnumerable<IndexStatisticModel> IndexStatisticModels,
        Dictionary<string, RunnableStatistic> RuntimeStatistics, long EntireDocCount, double EntireSizeInBytes);
}