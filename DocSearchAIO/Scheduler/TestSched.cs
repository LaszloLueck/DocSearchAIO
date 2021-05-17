using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using DocSearchAIO.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DocSearchAIO.Scheduler
{
    [DisallowConcurrentExecution]
    public class TestSched : IJob
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly ActorSystem _actorSystem;

        public TestSched(ILoggerFactory loggerFactory, IConfiguration configuration, ActorSystem actorSystem)
        {
            _logger = loggerFactory.CreateLogger<TestSched>();
            _configuration = configuration;
            _actorSystem = actorSystem;
        }
        
        public async Task Execute(IJobExecutionContext context)
        {
            await Task.Run(async () =>
            {
                var mat = _actorSystem.Materializer();
                var cfg = new ConfigurationObject();
                _configuration.GetSection("configurationObject").Bind(cfg);
                _logger.LogInformation("Puu: " + cfg.IndexName);
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