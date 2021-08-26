using System.Threading.Tasks;
using Quartz;

namespace DocSearchAIO.Scheduler
{
    public class MsgProcessingJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}