using System;
using DocSearchAIO.Configuration;
using DocSearchAIO.Scheduler;
using DocSearchAIO.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

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


            cfg.Processing.ForEach((schedulerKey, schedulerEntry) =>
            {
                services.AddQuartz(q =>
                {
                    var jk = new JobKey(schedulerEntry.JobName, cfg.GroupName);
                    switch (schedulerEntry.JobName)
                    {
                        case "wordProcessingJob":
                            q.AddJob<OfficeWordProcessingJob>(jk,
                                p => p.WithDescription($"job for processing and indexing {schedulerKey} documents"));
                            break;
                        case "excelProcessingJob":
                            q.AddJob<OfficeExcelProcessingJob>(jk,
                                p => p.WithDescription($"job for processing and indexing {schedulerKey} documents"));
                            break;
                        case "powerpointProcessingJob":
                            q.AddJob<OfficePowerpointProcessingJob>(jk,
                                p => p.WithDescription($"job for processing and indexing {schedulerKey} documents"));
                            break;
                        case "pdfProcessingJob":
                            q.AddJob<PdfProcessingJob>(jk,
                                p => p.WithDescription($"job for processing and indexing {schedulerKey} documents"));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(schedulerEntry.JobName),
                                schedulerEntry.JobName, "cannot build quartz job with the given scheduler entry");
                    }


                    q.AddTrigger(t => t
                        .ForJob(jk)
                        .WithIdentity(schedulerEntry.TriggerName, cfg.GroupName)
                        .StartAt(
                            DateBuilder.EvenSecondDate(DateTimeOffset.UtcNow.AddSeconds(schedulerEntry.StartDelay)))
                        .WithSimpleSchedule(x => x.WithIntervalInSeconds(schedulerEntry.RunsEvery).RepeatForever())
                        .WithDescription($"trigger for {schedulerKey}-processing and indexing")
                    );
                });
            });
            services.AddQuartzServer(options => options.WaitForJobsToComplete = true);
        }
    }
}