using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Util.Internal;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.TOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using Quartz.Listener;

namespace DocSearchAIO.DocSearch.Services
{
    public class SchedulerStatisticsService
    {
        private readonly ILogger _logger;
        private readonly ConfigurationObject _configurationObject;

        public SchedulerStatisticsService(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<SchedulerStatisticsService>();
            configuration.GetSection("configurationObject").Bind(_configurationObject);
        }

        public async Task<IEnumerable<SchedulerStatistics>> GetSchedulerStatistics()
        {
            var schedulerFactory = new StdSchedulerFactory();
            var resultTasks = (await schedulerFactory.GetAllSchedulers()).Select(async scheduler =>
            {
                var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
                var results = triggerKeys.Select(async trigger =>
                {
                    var result = new SchedulerStatistics();
                    result.TriggerName = trigger.Name;
                    result.GroupName = trigger.Group;

                    result.ProcessingState = _configurationObject.Processing
                        .Where(p => p.Value.TriggerName == trigger.Name).First().Value.Active;
                    
                    var trg = await scheduler.GetTrigger(trigger);
                    result.TriggerState = (await scheduler.GetTriggerState(trigger)).ToString();

                    if (trg != null)
                    {
                        result.NextFireTime = trg.GetNextFireTimeUtc()?.UtcDateTime.ToLocalTime();
                        result.Description = trg.Description;
                        result.StartTime = trg.StartTimeUtc.LocalDateTime;
                        result.LastFireTime = trg.GetPreviousFireTimeUtc()?.UtcDateTime.ToLocalTime();
                    }
                    return result;    
                });

                return await Task.WhenAll(results);
            });

            var tmp = (await Task.WhenAll(resultTasks)).SelectMany(i => i);
            return tmp;
        }
    }
}