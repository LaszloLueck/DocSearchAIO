using System.Linq;
using DocSearchAIO.Statistics;
using LiteDB;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Optional;

namespace DocSearchAIO.Scheduler
{
    public class StatisticUtilities
    {
        private readonly ILogger _logger;
        private readonly ILiteDatabase _liteDatabase;


        public StatisticUtilities(ILoggerFactory loggerFactory, ILiteDatabase liteDatabase)
        {
            _logger = loggerFactory.CreateLogger<StatisticUtilities>();
            _liteDatabase = liteDatabase;
        }

        public void AddJobStatisticToDatabase<TModel>(ProcessingJobStatistic jobStatistic)
        {
            var liteCollection = _liteDatabase.GetCollection<ProcessingJobStatistic>(typeof(TModel).Name);
            _logger.LogInformation("write statistic for {Type}", typeof(TModel).Name);
            var json = JsonConvert.SerializeObject(jobStatistic, Formatting.Indented);
            _logger.LogInformation("Write {Object}", json);
            liteCollection.Insert(jobStatistic);
        }

        public Option<ProcessingJobStatistic> GetLatestJobStatisticByModel<TModel>()
        {
            var liteCollection = _liteDatabase.GetCollection<ProcessingJobStatistic>(typeof(TModel).Name);
            _logger.LogInformation("get statistic for {Type}", typeof(TModel).Name);
            return liteCollection
                .FindAll()
                .OrderByDescending(d => d.StartJob)
                .First()
                .SomeNotNull();
        }
    }
}