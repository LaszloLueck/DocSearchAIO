using System;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Scheduler;
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

            cfg
                .Processing
                .DictionaryKeyExistsAction(nameof(WordElasticDocument), kv =>
                {
                    var scheduler = kv.Value;
                    services.AddQuartz(q =>
                    {
                        var jk = new JobKey(scheduler.JobName, cfg.GroupName);
                        q.AddJob<OfficeWordProcessingJob>(jk,
                            p => p.WithDescription("job for processing and indexing word documents"));

                        q.AddTrigger(t => t
                            .ForJob(jk)
                            .WithIdentity(scheduler.TriggerName, cfg.GroupName)
                            .StartAt(DateBuilder.EvenSecondDate(DateTimeOffset.UtcNow.AddSeconds(scheduler.StartDelay)))
                            .WithSimpleSchedule(x => x.WithIntervalInSeconds(scheduler.RunsEvery).RepeatForever())
                            .WithDescription("trigger for word-processing and indexing")
                        );
                    });
                });

            cfg
                .Processing
                .DictionaryKeyExistsAction(nameof(PowerpointElasticDocument), kv =>
                {
                    var scheduler = kv.Value;
                    services.AddQuartz(q =>
                    {
                        var jk = new JobKey(scheduler.JobName, cfg.GroupName);
                        q.AddJob<OfficePowerpointProcessingJob>(jk,
                            p => p.WithDescription("job for processing and indexing powerpoint documents"));

                        q.AddTrigger(t => t
                            .ForJob(jk)
                            .WithIdentity(scheduler.TriggerName, cfg.GroupName)
                            .StartAt(DateBuilder.EvenSecondDate(DateTimeOffset.Now.AddSeconds(scheduler.StartDelay)))
                            .WithSimpleSchedule(x => x.WithIntervalInSeconds(scheduler.RunsEvery).RepeatForever())
                            .WithDescription("trigger for powerpoint-processing and indexing")
                        );
                    });
                });

            cfg
                .Processing
                .DictionaryKeyExistsAction(nameof(PdfElasticDocument), kv =>
                {
                    var scheduler = kv.Value;
                    services.AddQuartz(q =>
                    {
                        var jk = new JobKey(scheduler.JobName, cfg.GroupName);
                        q.AddJob<PdfProcessingJob>(jk,
                            p => p.WithDescription("job for processing and indexing pdf documents"));
                        q.AddTrigger(t => t
                            .WithIdentity(scheduler.TriggerName, cfg.GroupName)
                            .ForJob(jk)
                            .StartAt(DateBuilder.EvenSecondDate(DateTimeOffset.Now.AddSeconds(scheduler.StartDelay)))
                            .WithSimpleSchedule(x => x.WithIntervalInSeconds(scheduler.RunsEvery).RepeatForever())
                            .WithDescription("trigger for pdf-processing and indexing")
                        );
                    });
                });

            services.AddQuartzServer(options => options.WaitForJobsToComplete = true);
        }
    }
}