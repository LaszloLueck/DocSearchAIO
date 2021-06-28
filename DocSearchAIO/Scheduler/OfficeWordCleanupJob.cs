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
    public class OfficeWordCleanupJob : IJob
    {
        private readonly ILogger _logger;
        private readonly ConfigurationObject _cfg;
        private readonly SchedulerUtilities _schedulerUtilities;
        private readonly ReverseComparerService<ComparerModelWord> _reverseComparerService;
        private readonly IElasticSearchService _elasticSearchService;
        private readonly ElasticUtilities _elasticUtilities;

        public OfficeWordCleanupJob(ILoggerFactory loggerFactory, IConfiguration configuration,
            IElasticSearchService elasticSearchService)
        {
            _logger = loggerFactory.CreateLogger<OfficeWordCleanupJob>();
            _cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_cfg);
            _schedulerUtilities = new SchedulerUtilities(loggerFactory);
            _reverseComparerService =
                new ReverseComparerService<ComparerModelWord>(loggerFactory, new ComparerModelWord(_cfg.ComparerDirectory));
            _elasticSearchService = elasticSearchService;
            _elasticUtilities = new ElasticUtilities(loggerFactory, elasticSearchService);
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await Task.Run(() =>
            {
                var configEntry = _cfg.Cleanup[nameof(WordCleanupDocument)];
                configEntry
                    .Active
                    .IfTrueFalse(
                        async () =>
                        {
                            await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler,
                                configEntry.TriggerName,
                                _cfg.CleanupGroupName, TriggerState.Paused);
                            _logger.LogWarning(
                                "skip cleanup of word documents because the scheduler is inactive per config");
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