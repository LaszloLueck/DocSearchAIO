using System;
using System.Threading.Tasks;
using Akka;
using Akka.Streams.Dsl;
using DocSearchAIO.Classes;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DocSearchAIO.Scheduler
{
    public class SchedulerUtilities
    {
        private static ILogger _logger;
        public SchedulerUtilities(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SchedulerUtilities>();
        }

        public readonly Func<IScheduler, string, string, TriggerState, Task> SetTriggerStateByUserAction =
            async (scheduler, triggerName, groupName, triggerState) =>
            {
                var currentTriggerKey = new TriggerKey(triggerName, groupName);
                var currentTriggerState = await scheduler.GetTriggerState(currentTriggerKey);
                _logger.LogInformation("current triggerstate is {Triggerstate}", currentTriggerState);
                _logger.LogInformation($"set triggerstate for trigger {triggerName} to {triggerState}");
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
                        throw new ArgumentOutOfRangeException(nameof(triggerState), triggerState, "cannot process with that trigger state");
                }
            };
        
    }

    public class ComparerObject
    {
        public string PathHash { get; set; }
        public string DocumentHash { get; set; }

        public string OriginalPath { get; set; }
    }
}