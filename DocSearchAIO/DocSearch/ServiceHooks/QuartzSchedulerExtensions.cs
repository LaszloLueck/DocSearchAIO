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

            cfg.Cleanup.ForEach((cleanupKey, cleanupEntry) =>
            {
                services.AddQuartz(q =>
                {
                    var jk = new JobKey(cleanupEntry.JobName, cfg.CleanupGroupName);
                    switch (cleanupEntry.JobName)
                    {
                        case "wordCleanupJob":
                            q.AddJob<OfficeWordCleanupJob>(jk,
                                p => p.WithDescription($"cleanup job for {cleanupKey} documents"));
                            break;
                        case "powerpointCleanupJob":
                            q.AddJob<OfficePowerpointCleanupJob>(jk,
                                p => p.WithDescription($"cleanup job for {cleanupKey} documents"));
                            break;
                        case "excelCleanupJob":
                            q.AddJob<OfficeExcelCleanupJob>(jk,
                                p => p.WithDescription($"cleanup job for {cleanupKey} documents"));
                            break;
                        case "pdfCleanupJob":
                            q.AddJob<PdfCleanupJob>(jk,
                                p => p.WithDescription($"cleanup job for {cleanupKey} documents"));
                            break;
                        case "msgCleanupJob":
                            q.AddJob<MsgCleanupJob>(jk, p => p.WithDescription($"cleanup job for {cleanupKey} documents"));
                            break;    
                        case "emlCleanupJob":
                            q.AddJob<EmlCleanupJob>(jk, p => p.WithDescription($"cleanup job for {cleanupKey} documents"));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(cleanupEntry.JobName),
                                cleanupEntry.JobName, "cannot build quartz job with the given scheduler entry");
                    }

                    q.AddTrigger(t => t
                        .ForJob(jk)
                        .WithIdentity(cleanupEntry.TriggerName, cfg.CleanupGroupName)
                        .StartAt(DateBuilder.EvenSecondDate(DateTimeOffset.UtcNow.AddSeconds(cleanupEntry.StartDelay)))
                        .WithSimpleSchedule(x => x.WithIntervalInSeconds(cleanupEntry.RunsEvery).RepeatForever())
                        .WithDescription($"trigger for {cleanupKey}-processing and indexing")
                    );
                });
            });


            cfg.Processing.ForEach((schedulerKey, schedulerEntry) =>
            {
                services.AddQuartz(q =>
                {
                    var jk = new JobKey(schedulerEntry.JobName, cfg.SchedulerGroupName);
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
                        case "msgProcessingJob":
                            q.AddJob<MsgProcessingJob>(jk, p => p.WithDescription($"job for processing and indexing {schedulerKey} documents"));
                            break;
                        case "emlProcessingJob":
                            q.AddJob<EmlProcessingJob>(jk, p => p.WithDescription($"job for processing and indexing {schedulerKey} documents"));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(schedulerEntry.JobName),
                                schedulerEntry.JobName, "cannot build quartz job with the given scheduler entry");
                    }
                    q.AddTrigger(t => t
                        .ForJob(jk)
                        .WithIdentity(schedulerEntry.TriggerName, cfg.SchedulerGroupName)
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