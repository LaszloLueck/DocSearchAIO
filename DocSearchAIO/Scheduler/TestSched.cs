using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using DocSearchAIO.Configuration;
using DocSearchAIO.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
using Quartz;

namespace DocSearchAIO.Scheduler
{
    [DisallowConcurrentExecution]
    public class TestSched : IJob
    {
        private readonly ILogger _logger;
        private readonly ConfigurationObject _cfg;
        private readonly ActorSystem _actorSystem;
        private readonly ElasticSearchService _elasticSearchService;
        private readonly SchedulerUtils _schedulerUtils;

        public TestSched(ILoggerFactory loggerFactory, IConfiguration configuration, ActorSystem actorSystem,
            IElasticClient elasticClient)
        {
            _logger = loggerFactory.CreateLogger<TestSched>();
            _cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(_cfg);
            _actorSystem = actorSystem;
            _elasticSearchService = new ElasticSearchService(loggerFactory, elasticClient);
            _schedulerUtils = new SchedulerUtils(loggerFactory);
        }
        
        public async Task Execute(IJobExecutionContext context)
        {
            await Task.Run(async () =>
            {
                var mat = _actorSystem.Materializer();
                _logger.LogInformation("Puu: " + _cfg.IndexName);
                var source = Source.From(Enumerable.Range(1, 10))
                                //.Buffer(2, OverflowStrategy.Backpressure)
                                .Select(i =>
                                {
                                    _logger.LogInformation($"A: {i}");
                                    return i;
                                })
                                .Async()
                                //.Buffer(2, OverflowStrategy.Backpressure)
                                .Select(i =>
                                {
                                    _logger.LogInformation($"B: {i}");
                                    return i;
                                })
                                .Async()
                                //.Buffer(2, OverflowStrategy.Backpressure)
                                .Select(i =>
                                {
                                    _logger.LogInformation($"C: {i}");
                                    return i;
                                })
                                .Async();
                var runnable = source.RunWith(Sink.Ignore<int>(), mat);
                await Task.WhenAll(runnable);

            });
        }
    }
}