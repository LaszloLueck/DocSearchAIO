using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace DocSearchAIO
{
    public class Program
    {
        public static readonly Dictionary<string, string> ArrayDict =
            new()
            {
                {"array:entries:0", "value0"},
                {"array:entries:1", "value1"},
                {"array:entries:2", "value2"},
                {"array:entries:4", "value4"},
                {"array:entries:5", "value5"}
            };

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddInMemoryCollection(ArrayDict);
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