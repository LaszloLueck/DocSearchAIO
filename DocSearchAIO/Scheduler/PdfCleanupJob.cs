using System.Threading.Tasks;
using Quartz;

namespace DocSearchAIO.Scheduler
{
    public class PdfCleanupJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            await Task.Run(() =>
            {
                
            });
        }
    }
}