using System.Threading.Tasks;
using Akka.Streams.Dsl;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DocSearchAIO.Scheduler
{
    public class PdfCleanupJob : IJob
    {
        private readonly ILogger _logger;
        private readonly ConfigurationObject _cfg;
        private readonly SchedulerUtilities _schedulerUtilities;
        private readonly IElasticSearchService _elasticSearchService;
        private readonly ElasticUtilities _elasticUtilities;
        private readonly ReverseComparerService<ComparerModelPdf> _reverseComparerService; 

        public PdfCleanupJob(ILoggerFactory loggerFactory, IConfiguration configuration, IElasticSearchService elasticSearchService)
        {
            _logger = loggerFactory.CreateLogger<PdfCleanupJob>();
            _cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_cfg);
            _schedulerUtilities = new SchedulerUtilities(loggerFactory);
            _elasticSearchService = elasticSearchService;
            _elasticUtilities = new ElasticUtilities(loggerFactory, elasticSearchService);
            _reverseComparerService = new ReverseComparerService<ComparerModelPdf>(loggerFactory, new ComparerModelPdf(_cfg.ComparerDirectory));
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await Task.Run(() =>
            {
                var configEntry = _cfg.Cleanup[nameof(PdfCleanupDocument)];
                configEntry
                    .Active
                    .IfTrueFalse(
                        async () =>
                        {
                            await _schedulerUtilities.SetTriggerStateByUserAction(context.Scheduler,
                                configEntry.TriggerName,
                                _cfg.CleanupGroupName, TriggerState.Paused);
                            _logger.LogWarning(
                                "skip cleanup of pdf documents because the scheduler is inactive per config");
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