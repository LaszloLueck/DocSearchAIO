using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using DocSearchAIO.Scheduler;
using Optional;
using Quartz;
using Quartz.Impl;

namespace DocSearchAIO.DocSearch.ServiceHooks
{
    public static class SchedulerUtils
    {
        public static async Task<Maybe<IScheduler>> GetStdSchedulerByName(string schedulerName)
        {
            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = await schedulerFactory.GetScheduler(schedulerName);
            return scheduler.MaybeValue();
        }

        public static async Task<IEnumerable<IScheduler>> GetAllScheduler()
        {
            return await new StdSchedulerFactory().GetAllSchedulers();
        }
    }
}