using System.Threading.Tasks;
using Akka.Actor;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DocSearchAIO.Scheduler
{
    public class OfficeExcelProcessingJob : IJob
    {
        private readonly ILogger _logger;
        private readonly ConfigurationObject _cfg;
        private readonly ActorSystem _actorSystem;
        private readonly IElasticSearchService _elasticSearchService;
        private readonly SchedulerUtilities _schedulerUtilities;
        private readonly StatisticUtilities<StatisticModelExcel> _statisticUtilities;
        private readonly ComparerModelWord _comparerModel;
        private readonly JobStateMemoryCache<MemoryCacheModelExcel> _jobStateMemoryCache;

        public OfficeExcelProcessingJob(ILoggerFactory loggerFactory, IConfiguration configuration,
            ActorSystem actorSystem, IElasticSearchService elasticSearchService,
            IMemoryCache memoryCache)
        {
            _logger = loggerFactory.CreateLogger<OfficeExcelProcessingJob>();
            _cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_cfg);
            _actorSystem = actorSystem;
            _elasticSearchService = elasticSearchService;
            _schedulerUtilities = new SchedulerUtilities(loggerFactory, elasticSearchService);
            _statisticUtilities = StatisticUtilitiesProxy.ExcelStatisticUtility(loggerFactory, _cfg.StatisticsDirectory,
                new StatisticModelExcel().GetStatisticFileName);
            _comparerModel = new ComparerModelWord(loggerFactory, _cfg.ComparerDirectory);
            _jobStateMemoryCache = JobStateMemoryCacheProxy.GetExcelJobStateMemoryCache(loggerFactory, memoryCache);
            _jobStateMemoryCache.RemoveCacheEntry();
        }
        
        
        public async Task Execute(IJobExecutionContext context)
        {
            await Task.Run(() =>
            {
                _logger.LogInformation("job running");
            });
        }
    }
}