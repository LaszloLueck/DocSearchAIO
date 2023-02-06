using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.Endpoints.Administration.Scheduler;
using DocSearchAIO.Utilities;
using LanguageExt;
using Quartz;
using Quartz.Impl.Matchers;

namespace DocSearchAIO.DocSearch.Services;

public interface ISchedulerStatisticsService
{
    public Task<IEnumerable<(TypedGroupNameString key, SchedulerStatistics statistics)>> SchedulerStatistics();
}

public class SchedulerStatisticsService : ISchedulerStatisticsService
{
    private readonly ILogger _logger;
    private readonly IConfigurationUpdater _configurationUpdater;

    public SchedulerStatisticsService(ILoggerFactory loggerFactory,
        IConfigurationUpdater configurationUpdater)
    {
        _logger = loggerFactory.CreateLogger<SchedulerStatisticsService>();
        _configurationUpdater = configurationUpdater;
    }

    private static readonly Func<IScheduler, SchedulerStatistics> StatisticsObject = scheduler =>
        new SchedulerStatistics(scheduler.SchedulerName, scheduler.SchedulerInstanceId,
            CalculateStateCheck(scheduler));

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
                configurationObject
                    .Processing
                    .Map(kv => (kv.Value.TriggerName, kv.Value.Active));
            var cleanup = configurationObject
                .Cleanup
                .Map(kv => (kv.Value.TriggerName, kv.Value.Active));
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
                    .Filter(r => r.TriggerName == trigger.Name)
                    .Map(d => d.Active)
                    .ToOption()
                    .IfNone(false), trg.ResolveNullable(string.Empty, (v, _) => v.JobKey.Name)
            );
        };

    private static readonly Func<IScheduler, TypedGroupNameString, Task<IReadOnlyCollection<TriggerKey>>>
        TriggerKeys = async (scheduler, groupName) =>
            await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.GroupEquals(groupName.Value));


    private static async IAsyncEnumerable<SchedulerTriggerStatisticElement> CalculateSchedulerTriggerStatisticsElements(
        IReadOnlyCollection<TriggerKey> triggerKeys, ConfigurationObject configurationObject, IScheduler scheduler)
    {
        foreach (var triggerKey in triggerKeys)
            yield return await SchedulerTriggerStatisticElement(Tuples(configurationObject), triggerKey, scheduler);
    }

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
                            var triggerKeys = await TriggerKeys(scheduler, groupName);
                            var innerResultTasks =
                                CalculateSchedulerTriggerStatisticsElements(triggerKeys, configurationObject,
                                    scheduler);

                            statistics.TriggerElements = innerResultTasks.ToEnumerable().ToSeq();
                            statistics.TriggerElements.ForEach(result =>
                            {
                                logger.LogInformation(
                                    "TriggerState: {TriggerState} : {TriggerName} : {TriggerGroup}",
                                    result.TriggerState, result.TriggerName, result.GroupName);
                            });

                            return statistics;
                        },
                        () => Task.FromResult(new SchedulerStatistics("", "", "")));

                return await t;
            };

    public async Task<IEnumerable<(TypedGroupNameString key, SchedulerStatistics statistics)>> SchedulerStatistics()
    {
        var cfg = await _configurationUpdater.ReadConfigurationAsync();
        var source = new List<TypedGroupNameString>
        {
            TypedGroupNameString.New(cfg.SchedulerGroupName),
            TypedGroupNameString.New(cfg.CleanupGroupName)
        };

        return await CalculateSchedulerStatistics(source, cfg);
    }

    private async Task<IEnumerable<(TypedGroupNameString, SchedulerStatistics)>> CalculateSchedulerStatistics(
        IEnumerable<TypedGroupNameString> source, ConfigurationObject cfg)
    {
        var tasks = source.Map(async groupNameString =>
        {
            var schedulerStatistic =
                await SchedulerStatistic(cfg, groupNameString, _logger);
            return (groupNameString, schedulerStatistic);
        });

        return await tasks.SequenceParallel(3);
    }
}