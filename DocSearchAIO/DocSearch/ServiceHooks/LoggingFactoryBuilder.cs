using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace DocSearchAIO.DocSearch.ServiceHooks
{
    
    public static class LoggingFactoryBuilder
    {
        private static ILoggerFactory CreateLoggingFactory()
        {
            return LoggerFactory.Create(builder =>
            {
                builder
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
        
        
        public static ILogger<T> Build<T>()
        {
            return CreateLoggingFactory().CreateLogger<T>();
        }
    }
    
}