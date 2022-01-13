using System.Collections.Immutable;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace DocSearchAIO
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var builder = ImmutableDictionary.CreateBuilder<string, string>();
                    builder.Add("array:entries:0", "value0");
                    builder.Add("array:entries:1", "value1");
                    builder.Add("array:entries:2", "value2");
                    builder.Add("array:entries:3", "value3");
                    builder.Add("array:entries:4", "value4");
                    builder.Add("array:entries:5", "value5");

                    config.AddInMemoryCollection(builder.ToImmutable());
                    config.AddJsonFile("Resources/config/config.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);
                    config.AddCommandLine(args);
                })
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .ConfigureLogging(logging =>
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
    }
}