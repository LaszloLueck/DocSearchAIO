using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using DocSearchAIO.Configuration;
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

        private static readonly Func<IScheduler, SchedulerStatistics> StatisticsObject = scheduler =>
            new SchedulerStatistics
            {
                SchedulerName = scheduler.SchedulerName,
                SchedulerInstanceId = scheduler.SchedulerInstanceId,
                State = scheduler.IsStarted ? "Gestartet" :
                    scheduler.IsShutdown ? "Heruntergefahren" :
                    scheduler.InStandbyMode ? "Pausiert" : "Unbekannt"
            };

        private static readonly Func<ConfigurationObject, IEnumerable<(string TriggerName, bool Active)>> Tuples =
            configurationObject =>
            {
                var processing = configurationObject.Processing.Select(d => (d.Value.TriggerName, d.Value.Active));
                var cleanup = configurationObject.Cleanup.Select(d => (d.Value.TriggerName, d.Value.Active));
                return processing.Concat(cleanup);
            };

        private static readonly
            Func<IEnumerable<(string TriggerName, bool Active)>, TriggerKey, IScheduler,
                Task<SchedulerTriggerStatisticElement>>
            SchedulerTriggerStatisticElement = async (tuples, trigger, scheduler) =>
            {
                var triggerState = await scheduler.GetTriggerState(trigger);
                var trg = await scheduler.GetTrigger(trigger);

                var tResult = new SchedulerTriggerStatisticElement
                {
                    ProcessingState = tuples
                        .Where(r => r.TriggerName == trigger.Name)
                        .Select(d => d.Active)
                        .TryFirst()
                        .Unwrap(),
                    TriggerName = trigger.Name,
                    GroupName = trigger.Group,
                    TriggerState = triggerState.ToString(),
                    NextFireTime = trg?.GetNextFireTimeUtc()?.UtcDateTime.ToLocalTime(),
                    Description = trg?.Description ?? string.Empty,
                    StartTime = trg?.StartTimeUtc.LocalDateTime,
                    LastFireTime = trg?.GetPreviousFireTimeUtc()?.UtcDateTime.ToLocalTime(),
                    JobName = trg?.JobKey.Name ?? string.Empty
                };


                return tResult;
            };

        private static readonly Func<IScheduler, TypedGroupNameString, Task<IReadOnlyCollection<TriggerKey>>>
            TriggerKeys = async (scheduler, groupName) =>
                await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.GroupEquals(groupName.Value));


        private static readonly Func<ConfigurationObject, TypedGroupNameString, ILogger, Task<SchedulerStatistics>>
            SchedulerStatistic =
                async (configurationObject, groupName, logger) =>
                {
                    var schedulerOpt = await SchedulerUtilities.StdSchedulerByName(configurationObject.SchedulerName);
                    var t = schedulerOpt
                        .Match(
                            async scheduler =>
                            {
                                var statistics = StatisticsObject(scheduler);
                                var innerResultTasks = (await TriggerKeys(scheduler, groupName))
                                    .Select(async trigger =>
                                        await SchedulerTriggerStatisticElement(Tuples(configurationObject), trigger,
                                            scheduler));

                                var results = (await innerResultTasks.WhenAll()).ToArray();

                                statistics.TriggerElements = results;

                                results.ForEach(result =>
                                {
                                    logger.LogInformation(
                                        "TriggerState: {TriggerState} : {TriggerName} : {TriggerGroup}",
                                        result.TriggerState, result.TriggerName, result.GroupName);
                                });

                                return statistics;
                            },
                            () => Task.Run(() => new SchedulerStatistics()));

                    return await t;
                };

        public async Task<Dictionary<string, SchedulerStatistics>> SchedulerStatistics()
        {
            var source = new List<TypedGroupNameString>
            {
                new(_configurationObject.SchedulerGroupName),
                new(_configurationObject.CleanupGroupName)
            };

            var resultTasks = source
                .Select(async groupNameSource =>
                {
                    var schedulerStatistic =
                        await SchedulerStatistic(_configurationObject, groupNameSource, _logger);
                    return new KeyValuePair<string, SchedulerStatistics>(groupNameSource.Value, schedulerStatistic);
                });

            return (await resultTasks.WhenAll()).ToDictionary();
        }
    }
}