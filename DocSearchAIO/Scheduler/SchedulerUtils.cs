using System.IO;
using System.Threading.Tasks;
using DocSearchAIO.Configuration;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.Scheduler
{
    public class SchedulerUtils
    {
        private readonly ILogger _logger;
        public SchedulerUtils(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SchedulerUtils>();
        }
        
        public async Task<string> CreateComparerDirectoryIfNotExists(SchedulerEntry schedulerEntry)
        {
            var compareDirectory = schedulerEntry.ComparerDirectory + schedulerEntry.ComparerFile;
            if (!File.Exists(compareDirectory))
            {
                if (!Directory.Exists(schedulerEntry.ComparerDirectory))
                {
                    _logger.LogWarning($"Comparer directory <{schedulerEntry.ComparerDirectory}> does not exist, lets create it");
                    Directory.CreateDirectory(schedulerEntry.ComparerDirectory);
                }
                _logger.LogWarning($"Comparerfile <{compareDirectory}> does not exists, lets create it");
                await File.Create(compareDirectory).DisposeAsync();
            }

            return await Task.Run(() => compareDirectory);
        }
    }
}