using System.Threading.Tasks;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DocSearchAIO.Scheduler
{
    public class OfficeExcelCleanupJob : IJob
    {
        private readonly ILogger _logger;
        private readonly ConfigurationObject _cfg;
        private readonly SchedulerUtilities _schedulerUtilities;
        private readonly ReverseComparerService<ComparerModelExcel> _reverseComparerService;
        private readonly IElasticSearchService _elasticSearchService;
        private readonly ElasticUtilities _elasticUtilities;
        public OfficeExcelCleanupJob(ILoggerFactory loggerFactory, IConfiguration configuration, IElasticSearchService elasticSearchService)
        {
            _logger = loggerFactory.CreateLogger<OfficeExcelCleanupJob>();
            _cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_cfg);
            _schedulerUtilities = new SchedulerUtilities(loggerFactory);
            _elasticSearchService = elasticSearchService;
            _elasticUtilities = new ElasticUtilities(loggerFactory, elasticSearchService);
            _reverseComparerService =
                new ReverseComparerService<ComparerModelExcel>(loggerFactory, new ComparerModelExcel(_cfg.ComparerDirectory));
        }
        
        public async Task Execute(IJobExecutionContext context)
        {
            await Task.Run(() =>
            {
                var configEntry = _cfg.Cleanup[nameof(ExcelCleanupDocument)];
                configEntry
                    .Active
                    .IfTrueFalse(
                        async () =>
                        {
                            await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler,
                                configEntry.TriggerName,
                                _cfg.CleanupGroupName, TriggerState.Paused);
                            _logger.LogWarning(
                                "skip cleanup of excel documents because the scheduler is inactive per config");
                        },

                        async () =>
                        {
                            await Task.Run(async () =>
                            {
                                _logger.LogInformation("start processing cleanup job");
                                var cleanupIndexName =
                                    _elasticUtilities.CreateIndexName(_cfg.IndexName, configEntry.ForIndexSuffix);
                                await _reverseComparerService.Process(cleanupIndexName);
                            });
                        }
                    );
            });
        }
    }
}