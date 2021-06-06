using DocSearchAIO.Statistics;
using LiteDB;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DocSearchAIO.Scheduler
{
    public class StatisticUtilities<T>
    {
        private readonly ILogger _logger;
        private readonly ILiteCollection<ProcessingJobStatistic> _liteCollection;
        
        
        public StatisticUtilities(ILoggerFactory loggerFactory, ILiteDatabase liteDatabase)
        {
            _logger = loggerFactory.CreateLogger<StatisticUtilities<T>>();
            _liteCollection = liteDatabase.GetCollection<ProcessingJobStatistic>(typeof(T).Name);
        }

        public void AddJobStatisticToDatabase(ProcessingJobStatistic jobStatistic)
        {
            _logger.LogInformation("write statistic for {Type}", typeof(T).Name);
            var json = JsonConvert.SerializeObject(jobStatistic, Formatting.Indented);
            _logger.LogInformation("Write {Object}", json);
            _liteCollection.Insert(jobStatistic);
        }
    }
}