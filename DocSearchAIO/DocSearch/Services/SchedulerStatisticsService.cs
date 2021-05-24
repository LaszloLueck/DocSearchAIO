using System;
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
            var tmpConfig = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(tmpConfig);
            _configurationObject = tmpConfig;
        }

        public async Task<IEnumerable<SchedulerStatistics>> GetSchedulerStatistics()
        {
            var schedulerFactory = new StdSchedulerFactory();
            var resultTasks = (await schedulerFactory.GetAllSchedulers()).Select(async scheduler =>
            {
                var schedulerStatistics = new SchedulerStatistics
                {
                    SchedulerName = scheduler.SchedulerName,
                    SchedulerInstanceId = scheduler.SchedulerInstanceId,
                    State = scheduler.IsStarted ? "Gestartet" :
                        scheduler.IsShutdown ? "Heruntergefahren" :
                        scheduler.InStandbyMode ? "Pausiert" : "Unbekannt"
                };

                var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
                
                var innerResultTasks = triggerKeys.Select(async trigger =>
                {
                    var result = new SchedulerTriggerStatisticElement
                    {
                        ProcessingState = _configurationObject
                            .Processing
                            .Where(r => r.Value.TriggerName == trigger.Name)
                            .Select(d => d.Value.Active)
                            .First(),
                        TriggerName = trigger.Name,
                        GroupName = trigger.Group
                    };


                    var trg = await scheduler.GetTrigger(trigger);
                    /*
                        blocked
			            complete
			            error
			            none
			            normal
			            paused
                     */
                    result.TriggerState = (await scheduler.GetTriggerState(trigger)).ToString();
                    

                    if (trg == null) return result;
                    result.NextFireTime = trg.GetNextFireTimeUtc()?.UtcDateTime.ToLocalTime();
                    result.Description = trg.Description;
                    result.StartTime = trg.StartTimeUtc.LocalDateTime;
                    result.LastFireTime = trg.GetPreviousFireTimeUtc()?.UtcDateTime.ToLocalTime();
                    result.JobName = trg.JobKey.Name;
                    return result;
                });

                var results = await Task.WhenAll(innerResultTasks);
                schedulerStatistics.TriggerElements = results;
                return schedulerStatistics;
            });

            return await Task.WhenAll(resultTasks);
        }
    }
}