using System;

using DocSearchAIO.Configuration;
using DocSearchAIO.Scheduler;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Quartz;
using Quartz.Impl;

namespace DocSearchAIO.DocSearch.ServiceHooks
{
    public static class QuartzSchedulerExtensions
    {
        public static void AddQuartzScheduler(this IServiceCollection services, IConfiguration configuration)
        {
            var cfg = new ConfigurationObject();
            configuration.GetSection("configurationObject").Bind(cfg);
            services.AddQuartz(q =>
            {
                q.SchedulerName = cfg.SchedulerName;
                q.SchedulerId = cfg.SchedulerId;
                q.UseMicrosoftDependencyInjectionJobFactory();
            });

            if (cfg.Processing.ContainsKey("word"))
            {
                var scheduler = cfg.Processing["word"];
                services.AddQuartz(q =>
                {
                    q.ScheduleJob<OfficeWordProcessingJob>(trigger => trigger
                        .WithIdentity(scheduler.TriggerName, scheduler.GroupName)
                        .StartAt(DateBuilder.EvenSecondDate(DateTimeOffset.UtcNow.AddSeconds(scheduler.StartDelay)))
                        .WithSimpleSchedule(x => x.WithIntervalInSeconds(scheduler.RunsEvery).RepeatForever())
                        .WithDescription("trigger for word-processing and indexing")
                    );
                });
            }

            if (cfg.Processing.ContainsKey("powerpoint"))
            {
                var scheduler = cfg.Processing["powerpoint"];
                services.AddQuartz(q =>
                {
                    q.ScheduleJob<OfficePowerpointProcessingJob>(trigger => trigger
                        .WithIdentity(scheduler.TriggerName, scheduler.GroupName)
                        .StartAt(DateBuilder.EvenSecondDate(DateTimeOffset.Now.AddSeconds(scheduler.StartDelay)))
                        .WithSimpleSchedule(x => x.WithIntervalInSeconds(scheduler.RunsEvery).RepeatForever())
                        .WithDescription("trigger for powerpoint-processing and indexing")
                    );
                });
            }

            if (cfg.Processing.ContainsKey("pdf"))
            {
                var scheduler = cfg.Processing["pdf"];
                services.AddQuartz(q =>
                {
                    q.ScheduleJob<PdfProcessingJob>(trigger => trigger
                        .WithIdentity(scheduler.TriggerName, scheduler.GroupName)
                        .StartAt(DateBuilder.EvenSecondDate(DateTimeOffset.Now.AddSeconds(scheduler.StartDelay)))
                        .WithSimpleSchedule(x => x.WithIntervalInSeconds(scheduler.RunsEvery).RepeatForever())
                        .WithDescription("trigger for pdf-processing and indexing")
                    );
                });
            }

            services.AddQuartzServer(options => options.WaitForJobsToComplete = true);
        }
    }
}
