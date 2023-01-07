global using static LanguageExt.Prelude;
using Akka.Actor;
using DocSearchAIO.Classes;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Console;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = false;
    options.SingleLine = true;
    options.TimestampFormat = "[yyy-MM-dd HH:mm:ss] ";
    options.ColorBehavior = LoggerColorBehavior.Enabled;
});
builder.Logging.AddFilter("*", LogLevel.Information);

// Add services to the container.
builder.Configuration.AddInMemoryCollection(new List<KeyValuePair<string, string?>>
{
    new("array:entries:0", "value0"),
    new("array:entries:1", "value1"),
    new("array:entries:2", "value2"),
    new("array:entries:3", "value3"),
    new("array:entries:4", "value4"),
    new("array:entries:5", "value5")
});
builder.Configuration.AddJsonFile("Resources/config/config.json", optional: false, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v2", new OpenApiInfo { Title = "DocSearchAIO", Version = "v2" });
    c.EnableAnnotations();
});

builder.Services.AddElasticSearch(builder.Configuration);
builder.Services.AddQuartzScheduler(builder.Configuration);
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton(_ => ActorSystem.Create("DocSearchAIOActorSystem"));
builder.Services.AddSingleton<IInitService>(x =>
    new InitService(x.GetRequiredService<IElasticSearchService>(), builder.Configuration));
builder.Services.AddSingleton<IFileDownloadService, FileDownloadService>();
builder.Services.AddSingleton<IDoSearchService>(x =>
    new DoSearchService(x.GetRequiredService<IElasticSearchService>(), x.GetRequiredService<ILoggerFactory>(),
        builder.Configuration));
builder.Services.AddSingleton<ISearchSuggestService>(x =>
    new SearchSuggestService(x.GetRequiredService<IElasticSearchService>(), x.GetRequiredService<ILoggerFactory>(),
        builder.Configuration));
builder.Services.AddSingleton<IDocumentDetailService>(x =>
    new DocumentDetailService(x.GetRequiredService<IElasticSearchService>(), x.GetRequiredService<ILoggerFactory>()));
builder.Services.AddSingleton<ISchedulerUtilities>(x => new SchedulerUtilities(x.GetRequiredService<ILoggerFactory>()));
builder.Services.AddSingleton<IElasticUtilities>(x =>
    new ElasticUtilities(x.GetRequiredService<ILoggerFactory>(), x.GetRequiredService<IElasticSearchService>()));
builder.Services.AddSingleton<ISchedulerStatisticsService>(x =>
    new SchedulerStatisticsService(x.GetRequiredService<ILoggerFactory>(), builder.Configuration));
builder.Services.AddSingleton<IAdministrationService>(x =>
    new AdministrationService(
        x.GetRequiredService<ILoggerFactory>(),
        builder.Configuration,
        x.GetRequiredService<IElasticSearchService>(),
        x.GetRequiredService<IMemoryCache>(),
        x.GetRequiredService<IElasticUtilities>(),
        x.GetRequiredService<ISchedulerStatisticsService>()));
builder.Services.AddSingleton<IOptionDialogService>(x =>
    new OptionDialogService(x.GetRequiredService<IElasticSearchService>(), builder.Configuration));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseSwagger(c => { c.SerializeAsV2 = true; });
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v2/swagger.json", "DocSearchAIO"); });
}

app.Lifetime.ApplicationStarted.Register(() => app.Services.GetService<ActorSystem>());
app.Lifetime.ApplicationStopping.Register(() => app.Services.GetService<ActorSystem>()?.Terminate().Wait());
app.MapHealthChecks("/healthz");

app.UseHttpsRedirection();
app.UseAuthorization();
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();

app.UseCors(x =>
    x.AllowAnyMethod()
        .AllowAnyHeader()
        .SetIsOriginAllowed(_ => true)
        .AllowCredentials()
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");

app.Run();