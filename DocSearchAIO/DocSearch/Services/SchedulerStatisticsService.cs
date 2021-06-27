using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Scheduler;
using DocSearchAIO.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Matchers;

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

        public async Task<Dictionary<string, SchedulerStatistics[]>> GetSchedulerStatistics()
        {
            var source = new List<GenericSourceGroupName>()
            {
                new(_configurationObject.SchedulerGroupName),
                new(_configurationObject.CleanupGroupName)
            };

            var tuples = _configurationObject.Cleanup
                .Select(d => (d.Value.TriggerName, d.Value.Active)).Concat(
                    _configurationObject.Processing.Select(d =>
                        (d.Value.TriggerName, d.Value.Active)));
            
            var resultTasks = source
                .Select(async groupNameSource =>
                {
                    var schedulerStatisticTasks =
                        (await SchedulerUtils.GetStdSchedulerByName(_configurationObject.SchedulerName))
                        .Unwrap(async scheduler =>
                        {
                            var statistics = new SchedulerStatistics
                            {
                                SchedulerName = scheduler.SchedulerName,
                                SchedulerInstanceId = scheduler.SchedulerInstanceId,
                                State = scheduler.IsStarted ? "Gestartet" :
                                    scheduler.IsShutdown ? "Heruntergefahren" :
                                    scheduler.InStandbyMode ? "Pausiert" : "Unbekannt"
                            };
                            var triggerKeys =
                                await scheduler.GetTriggerKeys(
                                    GroupMatcher<TriggerKey>.GroupEquals(groupNameSource.Value));

                            var innerResultTasks = triggerKeys.Select(async trigger =>
                            {
                                static SchedulerTriggerStatisticElement GetSchedulerTriggerStatisticElement(IEnumerable<(string TriggerName, bool Active)> tuples, TriggerKey trigger)
                                {
                                    return new()
                                    {
                                        ProcessingState = tuples
                                            .Where(r => r.TriggerName == trigger.Name)
                                            .Select(d => d.Active)
                                            .TryFirst()
                                            .Unwrap(),
                                        TriggerName = trigger.Name,
                                        GroupName = trigger.Group,
                                    };
                                }

                                var trg = await scheduler.GetTrigger(trigger);
                                /*
                                    blocked
                                    complete
                                    error
                                    none
                                    normal
                                    paused
                                 */
                                var triggerState = await scheduler.GetTriggerState(trigger);
                                var triggerStateElement = GetSchedulerTriggerStatisticElement(tuples, trigger);
                                triggerStateElement.TriggerState = triggerState.ToString();
                                
                                _logger.LogInformation(
                                    "TriggerState: {TriggerState} : {TriggerName} : {TriggerGroup}",
                                    triggerStateElement.TriggerState, trigger.Name, trigger.Group);

                                trg
                                    .MaybeValue()
                                    .Match(
                                        trgOpt =>
                                        {
                                            triggerStateElement.NextFireTime =
                                                trgOpt.GetNextFireTimeUtc()?.UtcDateTime.ToLocalTime();
                                            triggerStateElement.Description = trgOpt.Description;
                                            triggerStateElement.StartTime = trgOpt.StartTimeUtc.LocalDateTime;
                                            triggerStateElement.LastFireTime = trgOpt.GetPreviousFireTimeUtc()?.UtcDateTime
                                                .ToLocalTime();
                                            triggerStateElement.JobName = trgOpt.JobKey.Name;
                                            return triggerStateElement;
                                        },
                                        () => triggerStateElement
                                    );
                                return triggerStateElement;
                            });
                            var results = await Task.WhenAll(innerResultTasks);
                            statistics.TriggerElements = results;
                            return statistics;
                        });

                    var schedulerStatistics = await Task.WhenAll(schedulerStatisticTasks);
                    return new KeyValuePair<string, SchedulerStatistics[]>(groupNameSource.Value, schedulerStatistics);
                });

             return (await Task.WhenAll(resultTasks)).ToDictionary();

        }
    }
}