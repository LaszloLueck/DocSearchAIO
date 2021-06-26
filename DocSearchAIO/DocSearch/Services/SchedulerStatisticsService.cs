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
            var source = new[]
            {
                new GenericSourceGroupName(_configurationObject.SchedulerGroupName),
                new GenericSourceGroupName(_configurationObject.CleanupGroupName)
            };
            
            var resultTasks = source
                .Select(async groupNameSource =>
                {
                    var groupName = groupNameSource.Value;
                    var schedulerStatisticTasks = (await SchedulerUtils.GetStdSchedulerByName(_configurationObject.SchedulerName))
                        .Unwrap(async scheduler =>
                        {
                            var schedulerStatistics = new SchedulerStatistics
                            {
                                SchedulerName = scheduler.SchedulerName,
                                SchedulerInstanceId = scheduler.SchedulerInstanceId,
                                State = scheduler.IsStarted ? "Gestartet" :
                                    scheduler.IsShutdown ? "Heruntergefahren" :
                                    scheduler.InStandbyMode ? "Pausiert" : "Unbekannt"
                            };
                            var triggerKeys =
                                await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.GroupEquals(groupName));


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
                                    GroupName = trigger.Group,
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

                                trg
                                    .MaybeValue()
                                    .Match(
                                        trgOpt =>
                                        {
                                            result.NextFireTime =
                                                trgOpt.GetNextFireTimeUtc()?.UtcDateTime.ToLocalTime();
                                            result.Description = trgOpt.Description;
                                            result.StartTime = trgOpt.StartTimeUtc.LocalDateTime;
                                            result.LastFireTime = trgOpt.GetPreviousFireTimeUtc()?.UtcDateTime
                                                .ToLocalTime();
                                            result.JobName = trgOpt.JobKey.Name;
                                            return result;
                                        },
                                        () => result
                                    );
                                return result;
                            });
                            var results = await Task.WhenAll(innerResultTasks);
                            schedulerStatistics.TriggerElements = results;
                            return schedulerStatistics;
                        });

                    var schedulerStatistics =  await Task.WhenAll(schedulerStatisticTasks);
                    return new KeyValuePair<string, SchedulerStatistics[]>(groupName, schedulerStatistics);
                });

            var l = await Task.WhenAll(resultTasks);
            return l.ToDictionary();
            // var resultTasks = (await SchedulerUtils.GetAllScheduler()).Select(async scheduler =>
            // {
            //     var schedulerStatistics = new SchedulerStatistics
            //     {
            //         SchedulerName = scheduler.SchedulerName,
            //         SchedulerInstanceId = scheduler.SchedulerInstanceId,
            //         State = scheduler.IsStarted ? "Gestartet" :
            //             scheduler.IsShutdown ? "Heruntergefahren" :
            //             scheduler.InStandbyMode ? "Pausiert" : "Unbekannt"
            //     };
            //
            //     var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.GroupEquals(_configurationObject.SchedulerGroupName));
            //
            //     
            //     
            //     var innerResultTasks = triggerKeys.Select(async trigger =>
            //     {
            //         var result = new SchedulerTriggerStatisticElement
            //         {
            //             ProcessingState = _configurationObject
            //                 .Processing
            //                 .Where(r => r.Value.TriggerName == trigger.Name)
            //                 .Select(d => d.Value.Active)
            //                 .First(),
            //             TriggerName = trigger.Name,
            //             GroupName = trigger.Group,
            //             
            //         };
            //
            //
            //         var trg = await scheduler.GetTrigger(trigger);
            //         /*
            //             blocked
            //    complete
            //    error
            //    none
            //    normal
            //    paused
            //          */
            //         result.TriggerState = (await scheduler.GetTriggerState(trigger)).ToString();
            //
            //         trg
            //             .MaybeValue()
            //             .Match(
            //                 trgOpt =>
            //                 {
            //                     result.NextFireTime = trgOpt.GetNextFireTimeUtc()?.UtcDateTime.ToLocalTime();
            //                     result.Description = trgOpt.Description;
            //                     result.StartTime = trgOpt.StartTimeUtc.LocalDateTime;
            //                     result.LastFireTime = trgOpt.GetPreviousFireTimeUtc()?.UtcDateTime.ToLocalTime();
            //                     result.JobName = trgOpt.JobKey.Name;
            //                     return result;
            //                 },
            //                 () => result
            //             );
            //         return result;
            //     });
            //
            //     var results = await Task.WhenAll(innerResultTasks);
            //     schedulerStatistics.TriggerElements = results;
            //     return schedulerStatistics;
            // });
            //
            // return await Task.WhenAll(resultTasks);
        }
    }
}