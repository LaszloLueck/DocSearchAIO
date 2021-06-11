using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.Scheduler
{
    public class ProcessTimeMeasurement<T>
    {

        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch;
        

        public ProcessTimeMeasurement(ILoggerFactory loggerFactory)
        {
            _stopwatch = new Stopwatch();
            _logger = loggerFactory.CreateLogger<ProcessEvent<T>>();
            var processEvent = new ProcessEvent<T>(loggerFactory);
            processEvent.JobStarted += ProcessStarted;
            processEvent.JobCompleted += ProcessCompleted;
        }

        public long GetElapsedValueMs => _stopwatch.ElapsedMilliseconds;

        private void ProcessCompleted(object sender, EventArgs e)
        {
            _logger.LogInformation("event {Name} fired", "ProcessCompleted");
            _stopwatch.Stop();
        }

        private void ProcessStarted(object sender, EventArgs e)
        {
            _logger.LogInformation("event {Name} fired");
            _stopwatch.Start();
        }
        

    }
}