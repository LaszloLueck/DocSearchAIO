using System.Text.Json;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.Statistics;
using LanguageExt;

namespace DocSearchAIO.Classes;

public abstract class StatisticModel
{
    private readonly ILogger? _logger;

    protected abstract string DerivedModelName { get; }
    protected abstract TypedDirectoryPathString StatisticsDirectory { get; }

    public TypedFileNameString StatisticFileName => TypedFileNameString.New($"statistics_{DerivedModelName}.txt");

    protected StatisticModel()
    {
        _logger = LoggingFactoryBuilder.Build<StatisticModel>();
    }

    public Option<ProcessingJobStatistic> LatestJobStatisticByModel()
    {
        var filePath = $"{StatisticsDirectory}/{StatisticFileName}";
        _logger?.LogInformation("load statistics information from {FilePath} for model {DerivedModelName}",
            filePath,
            DerivedModelName);
        if (!File.Exists(filePath))
        {
            _logger?.LogWarning("statistic file <{FilePath}> does not exist. Lets create them", filePath);
            File.Create(filePath).Close();
        }

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

public sealed class StatisticModelWord : StatisticModel
{
    protected override string DerivedModelName => GetType().Name;
    protected override TypedDirectoryPathString StatisticsDirectory { get; } = TypedDirectoryPathString.New("");

    public StatisticModelWord()
    {
    }

    public StatisticModelWord(TypedDirectoryPathString statisticsDirectory)
    {
        StatisticsDirectory = statisticsDirectory;
    }
}

public sealed class StatisticModelPowerpoint : StatisticModel
{
    protected override string DerivedModelName => GetType().Name;
    protected override TypedDirectoryPathString StatisticsDirectory { get; } = TypedDirectoryPathString.New("");

    public StatisticModelPowerpoint()
    {
    }

    public StatisticModelPowerpoint(TypedDirectoryPathString statisticsDirectory)
    {
        StatisticsDirectory = statisticsDirectory;
    }
}

public sealed class StatisticModelPdf : StatisticModel
{
    protected override string DerivedModelName => GetType().Name;
    protected override TypedDirectoryPathString StatisticsDirectory { get; } = TypedDirectoryPathString.New("");

    public StatisticModelPdf()
    {
    }

    public StatisticModelPdf(TypedDirectoryPathString statisticsDirectory)
    {
        StatisticsDirectory = statisticsDirectory;
    }
}

public sealed class StatisticModelExcel : StatisticModel
{
    protected override string DerivedModelName => GetType().Name;
    protected override TypedDirectoryPathString StatisticsDirectory { get; } = TypedDirectoryPathString.New("");

    public StatisticModelExcel()
    {
    }

    public StatisticModelExcel(TypedDirectoryPathString statisticsDirectory)
    {
        StatisticsDirectory = statisticsDirectory;
    }
}

public sealed class StatisticModelMsg : StatisticModel
{
    protected override string DerivedModelName => GetType().Name;
    protected override TypedDirectoryPathString StatisticsDirectory { get; } = TypedDirectoryPathString.New("");

    public StatisticModelMsg() { }

    public StatisticModelMsg(TypedDirectoryPathString statisticsDirectory)
    {
        StatisticsDirectory = statisticsDirectory;
    }
}

public sealed class StatisticModelEml : StatisticModel
{
    protected override string DerivedModelName => GetType().Name;

    protected override TypedDirectoryPathString StatisticsDirectory { get; } = TypedDirectoryPathString.New("");

    public StatisticModelEml() { }

    public StatisticModelEml(TypedDirectoryPathString statisticsDirectory)
    {
        StatisticsDirectory = statisticsDirectory;
    }

}