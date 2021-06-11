using System;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.Scheduler
{

    public class ProcessEvent<T>
    {
        private readonly ILogger _logger;

        public ProcessEvent(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ProcessEvent<T>>();
        }
        
        public event EventHandler JobCompleted;
        public event EventHandler JobStarted;

        public void SetJobStarted(T cls)
        {
            _logger.LogInformation("get a job started event from {T}", cls.GetType().Name);
            JobStarted?.Invoke(cls, EventArgs.Empty);
        }

        public void SetJobCompleted(T cls)
        {
            _logger.LogInformation("get a job completed event from {T}", cls.GetType().Name);
            JobCompleted?.Invoke(cls, EventArgs.Empty);
        }
        
        
    }
    public sealed class JobEvents<T> : ProcessEvent<T>
    {
        public JobEvents(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
        }
    }
}