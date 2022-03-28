using System.Collections.Immutable;
using Akka.Actor;
using DocSearchAIO.DocSearch.ServiceHooks;
using Microsoft.Extensions.Logging.Console;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureLogging(logging =>
{
    logging
        .AddSimpleConsole(options =>
        {
            options.IncludeScopes = false;
            options.SingleLine = true;
            options.TimestampFormat = "[yyy-MM-dd HH:mm:ss] ";
            options.ColorBehavior = LoggerColorBehavior.Enabled;
        })
        .AddFilter("*", LogLevel.Information);
});

// Add services to the container.
var builder1 = ImmutableDictionary.CreateBuilder<string, string>();
builder1.Add("array:entries:0", "value0");
builder1.Add("array:entries:1", "value1");
builder1.Add("array:entries:2", "value2");
builder1.Add("array:entries:3", "value3");
builder1.Add("array:entries:4", "value4");
builder1.Add("array:entries:5", "value5");
builder.Configuration.AddInMemoryCollection(builder1.ToImmutable());
builder.Configuration.AddJsonFile("Resources/config/config.json", optional: false, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddElasticSearch(builder.Configuration);
builder.Services.AddQuartzScheduler(builder.Configuration);
builder.Services.AddMemoryCache();
builder.Services.AddSingleton(_ => ActorSystem.Create("DocSearchAIOActorSystem"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Lifetime.ApplicationStarted.Register(() => app.Services.GetService<ActorSystem>());
app.Lifetime.ApplicationStopping.Register(() => app.Services.GetService<ActorSystem>()?.Terminate().Wait());

app.UseHttpsRedirection();
app.UseAuthorization();
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();

app.UseCors(x =>
    x.AllowAnyMethod()
        .AllowAnyHeader()
        .SetIsOriginAllowed(origin => true)
        .AllowCredentials()
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");

app.Run();