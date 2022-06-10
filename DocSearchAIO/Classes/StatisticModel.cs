using System.Text.Json;
using DocSearchAIO.Statistics;
using LanguageExt;

namespace DocSearchAIO.Classes;

public abstract class StatisticModel
{
    private readonly ILogger? _logger;

    protected abstract string DerivedModelName { get; }
    protected abstract TypedDirectoryPathString StatisticsDirectory { get; }

    public TypedFileNameString StatisticFileName => new($"statistics_{DerivedModelName}.txt");

    protected StatisticModel(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<StatisticModel>();
    }

    protected StatisticModel()
    {
    }

    public Option<ProcessingJobStatistic> LatestJobStatisticByModel()
    {
        var filePath = $"{StatisticsDirectory}/{StatisticFileName}";
        _logger?.LogInformation("load statistics information from {FilePath} for model {DerivedModelName}",
            filePath,
            DerivedModelName);
        var content = File.ReadAllText(filePath);
        if (!content.Any())
            return Option<ProcessingJobStatistic>.None;
        try
        {
            var model = JsonSerializer.Deserialize<ProcessingJobStatistic>(content);
            return model!;
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "error occured while converting statistic json to model");
            return Option<ProcessingJobStatistic>.None;
        }
    }
}

public class StatisticModelWord : StatisticModel
{
    protected override string DerivedModelName => GetType().Name;
    protected override TypedDirectoryPathString StatisticsDirectory { get; } = new("");

    public StatisticModelWord()
    {
    }

    public StatisticModelWord(ILoggerFactory loggerFactory, TypedDirectoryPathString statisticsDirectory) :
        base(loggerFactory)
    {
        StatisticsDirectory = statisticsDirectory;
    }
}

public class StatisticModelPowerpoint : StatisticModel
{
    protected override string DerivedModelName => GetType().Name;
    protected override TypedDirectoryPathString StatisticsDirectory { get; } = new("");

    public StatisticModelPowerpoint()
    {
    }

    public StatisticModelPowerpoint(ILoggerFactory loggerFactory, TypedDirectoryPathString statisticsDirectory) :
        base(loggerFactory)
    {
        StatisticsDirectory = statisticsDirectory;
    }
}

public class StatisticModelPdf : StatisticModel
{
    protected override string DerivedModelName => GetType().Name;
    protected override TypedDirectoryPathString StatisticsDirectory { get; } = new("");

    public StatisticModelPdf()
    {
    }

    public StatisticModelPdf(ILoggerFactory loggerFactory, TypedDirectoryPathString statisticsDirectory) :
        base(loggerFactory)
    {
        StatisticsDirectory = statisticsDirectory;
    }
}

public class StatisticModelExcel : StatisticModel
{
    protected override string DerivedModelName => GetType().Name;
    protected override TypedDirectoryPathString StatisticsDirectory { get; } = new("");

    public StatisticModelExcel()
    {
    }

    public StatisticModelExcel(ILoggerFactory loggerFactory, TypedDirectoryPathString statisticsDirectory) :
        base(loggerFactory)
    {
        StatisticsDirectory = statisticsDirectory;
    }
}