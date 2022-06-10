using System.Collections.Concurrent;
using Akka;
using Akka.Streams.Dsl;
using DocSearchAIO.Utilities;
using LanguageExt;

namespace DocSearchAIO.Classes;

public static class ComparerHelper
{
    public static readonly Func<string, Source<ComparerObject, NotUsed>> GetComparerObjectSource = path => File
        .ReadAllLines(path)
        .AsParallel()
        .WithDegreeOfParallelism(10)
        .Map(ConvertLine)
        .Somes()
        .AsAkkaSource();

    private static Source<TIn, NotUsed> AsAkkaSource<TIn>(this IEnumerable<TIn> ieNumerable)
    {
        return Source.From(ieNumerable);
    }

    private static readonly Func<string, Option<ComparerObject>> ConvertLine = line =>
    {
        var spl = line.Split(";");
        if (spl.Length != 3) return Option<ComparerObject>.None;
        var cpo = new ComparerObject(spl[1], spl[0], spl[2]);
        return cpo;
    };

    public static readonly Func<string, ILogger, ConcurrentDictionary<string, ComparerObject>>
        FillConcurrentDictionary =
            (path, logger) =>
            {
                if (File.Exists(path))
                {
                    var retDictionary = File
                        .ReadAllLines(path)
                        .AsParallel()
                        .WithDegreeOfParallelism(10)
                        .Select(ConvertLine)
                        .Somes()
                        .Select(cpo => KeyValuePair.Create(cpo.PathHash, cpo))
                        .ToDictionary();
                    return new ConcurrentDictionary<string, ComparerObject>(retDictionary);
                }
                else
                {
                    logger.LogWarning("Cannot read Comparer file <{Path}> it does not exist, gave up", path);
                    return new ConcurrentDictionary<string, ComparerObject>();
                }
            };

    public static void RemoveComparerFile(string fileName)
    {
        File.Delete(fileName);
    }

    public static void CreateComparerFile(string fileName)
    {
        File.Create(fileName).Dispose();
    }

    public static bool CheckIfFileExists(string fileName)
    {
        return File.Exists(fileName);
    }

    public static bool CheckIfDirectoryExists(string directoryName)
    {
        return Directory.Exists(directoryName);
    }

    public static void CreateDirectory(string directoryName)
    {
        Directory.CreateDirectory(directoryName);
    }

    public static async Task WriteAllLinesAsync(ConcurrentDictionary<string, ComparerObject> cacheObject,
        string fileName)
    {
        await File.WriteAllLinesAsync(fileName,
            cacheObject.SelectKv((_, value) =>
                $"{value.DocumentHash};{value.PathHash};{value.OriginalPath}"));
    }
}