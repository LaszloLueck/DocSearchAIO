using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using DocSearchAIO.Classes;
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
using IScheduler = Quartz.IScheduler;

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
                    q.UseMicrosoftDependencyInjectionJobFactory();
                    q.ScheduleJob<OfficeWordProcessingJob>(trigger => trigger
                        .WithIdentity(scheduler.TriggerName, scheduler.GroupName)
                        .StartAt(DateBuilder.EvenSecondDate(DateTimeOffset.UtcNow.AddSeconds(scheduler.StartDelay)))
                        .WithSimpleSchedule(x => x.WithIntervalInSeconds(scheduler.RunsEvery).RepeatForever())
                        .WithDescription("trigger for word-processing and indexing")
                    );
                    q.UseMicrosoftDependencyInjectionJobFactory();
                });
            }
            //implement powerpoint scheduler
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
                    q.UseMicrosoftDependencyInjectionJobFactory();
                });
            }
           
            //implement pdf scheduler
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
                    q.UseMicrosoftDependencyInjectionJobFactory();
                });
            }

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
            app.UseStaticFiles();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}