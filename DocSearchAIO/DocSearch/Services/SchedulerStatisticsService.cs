﻿using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Utilities;
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
            new SchedulerStatistics(scheduler.SchedulerName, scheduler.SchedulerInstanceId, CalculateStateCheck(scheduler));

        private static string CalculateStateCheck(IScheduler scheduler)
        {
            if (scheduler.IsStarted)
                return "Gestartet";

            if (scheduler.IsShutdown)
                return "Heruntergefahren";

            return scheduler.InStandbyMode ? "Pausiert" : "Unbekannt";
        }

        private static readonly Func<ConfigurationObject, IEnumerable<(string TriggerName, bool Active)>> Tuples =
            configurationObject =>
            {
                var processing =
                    configurationObject.Processing.SelectKv((_, value) => (value.TriggerName, value.Active));
                var cleanup = configurationObject.Cleanup.SelectKv((_, value) => (value.TriggerName, value.Active));
                return processing.Concat(cleanup);
            };

        private static readonly
            Func<IEnumerable<(string TriggerName, bool Active)>, TriggerKey, IScheduler,
                Task<SchedulerTriggerStatisticElement>>
            SchedulerTriggerStatisticElement = async (tuples, trigger, scheduler) =>
            {
                var triggerState = await scheduler.GetTriggerState(trigger);
                var trg = await scheduler.GetTrigger(trigger);
                return new SchedulerTriggerStatisticElement(
                    trigger.Name, trigger.Group, trg.ResolveNullable(DateTime.Now,
                        (v, a) => v.GetNextFireTimeUtc()?.UtcDateTime.ToLocalTime() ?? a),
                    trg.ResolveNullable(DateTime.Now, (v, _) => v.StartTimeUtc.LocalDateTime),
                    trg.ResolveNullable(DateTime.Now,
                        (v, a) => v.GetPreviousFireTimeUtc()?.UtcDateTime.ToLocalTime() ?? a), triggerState.ToString(),
                    trg.ResolveNullable(string.Empty, (v, a) => v.Description ?? a), tuples
                        .Where(r => r.TriggerName == trigger.Name)
                        .Select(d => d.Active)
                        .TryFirst()
                        .GetValueOrDefault(), trg.ResolveNullable(string.Empty, (v, _) => v.JobKey.Name)
                );
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
                            () => Task.Run(() => new SchedulerStatistics("", "", "")));

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