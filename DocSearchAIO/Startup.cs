using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.Scheduler;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Quartz;
using Quartz.Impl;

namespace DocSearchAIO
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var cfg = new ConfigurationObject();
            Configuration.GetSection("configurationObject").Bind(cfg);
            
            services.AddControllers();
            services.AddRazorPages();
            services.AddScoped<ViewToStringRenderer, ViewToStringRenderer>();
            services.AddElasticSearch(Configuration);
            services.AddSingleton(_ => ActorSystem.Create("DocSearchActorSystem"));
            
            //implement word scheduler
            if (cfg.Processing.ContainsKey("word"))
            {
                var scheduler = cfg.Processing["word"];
                services.AddQuartz(q =>
                {
                    q.ScheduleJob<OfficeWordProcessingJob>(trigger => trigger
                        .WithIdentity(scheduler.TriggerName)
                        .StartAt(DateBuilder.EvenSecondDate(DateTimeOffset.Now.AddSeconds(scheduler.StartDelay)))
                        .WithDailyTimeIntervalSchedule(x => x.WithInterval(scheduler.RunsEvery, IntervalUnit.Second))
                        .WithDescription("trigger for word-processing and indexing")
                        .ForJob(scheduler.JobName)
                    );
                });
            }
            
            // services.AddQuartz(q =>
            // {
            //     q.ScheduleJob<TestSched>(trigger => trigger
            //         .WithIdentity("Combined Configuration Trigger")
            //         .StartAt(DateBuilder.EvenSecondDate(DateTimeOffset.UtcNow.AddSeconds()))
            //         .WithDailyTimeIntervalSchedule(x => x.WithInterval(10, IntervalUnit.Second))
            //         .WithDescription("my awesome trigger configured for a job with single call")
            //     );
            //     q.UseMicrosoftDependencyInjectionJobFactory();
            // });
            services.AddQuartzServer(options => options.WaitForJobsToComplete = true);
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "DocSearchAIO", Version = "v1"});
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DocSearchAIO v1"));
            }

            lifetime.ApplicationStarted.Register(() => app.ApplicationServices.GetService<ActorSystem>());
            lifetime.ApplicationStopping.Register(() =>
                app.ApplicationServices.GetService<ActorSystem>().Terminate().Wait());
            
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}