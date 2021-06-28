using System.Threading.Tasks;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DocSearchAIO.Scheduler
{
    public class OfficePowerpointCleanupJob : IJob
    {
        private readonly ILogger _logger;
        private readonly ConfigurationObject _cfg;
        private readonly SchedulerUtilities _schedulerUtilities;
        private readonly ReverseComparerService<ComparerModelPowerpoint> _reverseComparerService;
        private readonly IElasticSearchService _elasticSearchService;
        private readonly ElasticUtilities _elasticUtilities;

        public OfficePowerpointCleanupJob(ILoggerFactory loggerFactory, IConfiguration configuration,
            IElasticSearchService elasticSearchService)
        {
            _logger = loggerFactory.CreateLogger<OfficePowerpointCleanupJob>();
            _cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_cfg);
            _schedulerUtilities = new SchedulerUtilities(loggerFactory);
            _elasticSearchService = elasticSearchService;
            _elasticUtilities = new ElasticUtilities(loggerFactory, elasticSearchService);
            _reverseComparerService =
                new ReverseComparerService<ComparerModelPowerpoint>(loggerFactory, new ComparerModelPowerpoint(_cfg.ComparerDirectory));
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await Task.Run(() =>
            {
                var configEntry = _cfg.Cleanup[nameof(PowerpointCleanupDocument)];
                configEntry
                    .Active
                    .IfTrueFalse(
                        async () =>
                        {
                            await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler,
                                configEntry.TriggerName,
                                _cfg.CleanupGroupName, TriggerState.Paused);
                            _logger.LogWarning(
                                "skip cleanup of powerpoint documents because the scheduler is inactive per config");
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