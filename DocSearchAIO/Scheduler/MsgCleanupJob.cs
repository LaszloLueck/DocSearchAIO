using System.Threading.Tasks;
using Quartz;

namespace DocSearchAIO.Scheduler
{
    public class MsgCleanupJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}