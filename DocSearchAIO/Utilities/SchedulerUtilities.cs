using LanguageExt;
using LanguageExt.SomeHelp;
using Quartz;
using Quartz.Impl;

namespace DocSearchAIO.Utilities;

public interface ISchedulerUtilities
{
    public Task SetTriggerStateByUserAction(IScheduler scheduler, string triggerName, string groupName,
        TriggerState triggerState);
}

public class SchedulerUtilities : ISchedulerUtilities
{
    private readonly ILogger _logger;

    public SchedulerUtilities(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<SchedulerUtilities>();
    }

    public async Task SetTriggerStateByUserAction(IScheduler scheduler, string triggerName, string groupName, TriggerState triggerState)
    {
        var currentTriggerKey = new TriggerKey(triggerName, groupName);
        var currentTriggerState = await scheduler.GetTriggerState(currentTriggerKey);
        _logger.LogInformation("current trigger state is {TriggerState}", currentTriggerState);
        _logger.LogInformation("set trigger state for trigger {TriggerName} to {TriggerState}", triggerState,
            triggerName);
        switch (triggerState)
        {
            case TriggerState.Paused:
                await scheduler.PauseTrigger(currentTriggerKey);
                break;
            case TriggerState.Normal:
                await scheduler.ResumeTrigger(currentTriggerKey);
                break;
            case TriggerState.Complete:
                break;
            case TriggerState.Error:
                break;
            case TriggerState.Blocked:
                break;
            case TriggerState.None:
                break;
            default:
                throw new ArgumentOutOfRangeException($"cannot process with that trigger state {triggerState}");
        }
    }

    public static async Task<Option<IScheduler>> StdSchedulerByName(string schedulerName)
    {
        var schedulerFactory = new StdSchedulerFactory();
        var schedulerOpt = await schedulerFactory.GetScheduler(schedulerName);
        return schedulerOpt?.ToSome() ?? Option<IScheduler>.None;
    }
}