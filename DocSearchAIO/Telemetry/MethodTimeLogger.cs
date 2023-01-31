using System.Reflection;

namespace DocSearchAIO.Telemetry;

public static class MethodTimeLogger
{
    public static ILogger? Logger;

    public static void Log(MethodBase methodBase, long milliseconds, string message)
    {
        if (Logger is not null)
        {
            Logger!.LogInformation("{Class}.{Method} :: {Elapsed}ms", methodBase.DeclaringType!.Name, methodBase.Name,
                milliseconds);
        }
        else
        {
            Console.WriteLine($"{methodBase.DeclaringType!.Name}.{methodBase.Name} :: {milliseconds} ms");
        }
    }
}