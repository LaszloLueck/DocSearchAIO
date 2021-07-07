using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using DocSearchAIO.Utilities;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;

namespace DocSearchAIO.Scheduler
{
    public class SchedulerUtilities
    {
        private static ILogger? _logger;

        public SchedulerUtilities(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SchedulerUtilities>();
        }

        public readonly Func<IScheduler, string, string, TriggerState, Task> SetTriggerStateByUserAction =
            async (scheduler, triggerName, groupName, triggerState) =>
            {
                var currentTriggerKey = new TriggerKey(triggerName, groupName);
                var currentTriggerState = await scheduler.GetTriggerState(currentTriggerKey);
                _logger.LogInformation("current trigger state is {Triggerstate}", currentTriggerState);
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
                        throw new ArgumentOutOfRangeException(nameof(triggerState), triggerState,
                            "cannot process with that trigger state");
                }
            };

        public static async Task<Maybe<IScheduler>> StdSchedulerByName(string schedulerName)
        {
            var schedulerFactory = new StdSchedulerFactory();
            var schedulerOpt = await schedulerFactory.GetScheduler(schedulerName);
            return schedulerOpt == null ? Maybe<IScheduler>.None : Maybe<IScheduler>.From(schedulerOpt); 
        }
    }
}