using System.Threading.Tasks;
using Quartz;

namespace DocSearchAIO.Scheduler
{
    public class EmlCleanupJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}