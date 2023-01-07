using System.Text.Json;
using DocSearchAIO.Classes;
using DocSearchAIO.Statistics;
using LanguageExt;

namespace DocSearchAIO.Utilities;

public static class StatisticUtilitiesProxy
{
    public static readonly Func<ILoggerFactory, TypedDirectoryPathString,
            Seq<(IProcessorBase, Func<StatisticModel>)>>
        AsIEnumerable = (loggerFactory, statisticsPath) => Seq<(IProcessorBase, Func<StatisticModel>)>()
            .Add((new ProcessorBaseWord(), () => new StatisticModelWord(loggerFactory, statisticsPath)))
            .Add((new ProcessorBasePowerpoint(), () => new StatisticModelPowerpoint(loggerFactory, statisticsPath)))
            .Add((new ProcessorBasePdf(), () => new StatisticModelPdf(loggerFactory, statisticsPath)))
            .Add((new ProcessorBaseExcel(), () => new StatisticModelExcel(loggerFactory, statisticsPath)))
            .Add((new ProcessorBaseMsg(), () => new StatisticModelMsg(loggerFactory, statisticsPath)))
            .Add((new ProcessorBaseEml(), () => new StatisticModelEml(loggerFactory, statisticsPath)));

    public static readonly Func<ILoggerFactory, TypedDirectoryPathString, TypedFileNameString,
            StatisticUtilities<StatisticModelWord>>
        WordStatisticUtility = (loggerFactory, statisticsDirectory, statisticsFile) =>
            new StatisticUtilities<StatisticModelWord>(loggerFactory, statisticsDirectory, statisticsFile);

    public static readonly Func<ILoggerFactory, TypedDirectoryPathString, TypedFileNameString,
            StatisticUtilities<StatisticModelPowerpoint>>
        PowerpointStatisticUtility = (loggerFactory, statisticsDirectory, statisticsFile) =>
            new StatisticUtilities<StatisticModelPowerpoint>(loggerFactory, statisticsDirectory, statisticsFile);

    public static readonly Func<ILoggerFactory, TypedDirectoryPathString, TypedFileNameString,
            StatisticUtilities<StatisticModelPdf>>
        PdfStatisticUtility =
            (loggerFactory, statisticsDirectory, statisticsFile) =>
                new StatisticUtilities<StatisticModelPdf>(loggerFactory, statisticsDirectory, statisticsFile);

    public static readonly Func<ILoggerFactory, TypedDirectoryPathString, TypedFileNameString,
            StatisticUtilities<StatisticModelExcel>>
        ExcelStatisticUtility = (loggerFactory, statisticDirectory, statisticsFile) =>
            new StatisticUtilities<StatisticModelExcel>(loggerFactory, statisticDirectory, statisticsFile);

    public static readonly
        Func<ILoggerFactory, TypedDirectoryPathString, TypedFileNameString, StatisticUtilities<StatisticModelMsg>>
        MsgStatisticUtility = (loggerFactory, statisticDirectory, statisticsFile) =>
            new StatisticUtilities<StatisticModelMsg>(loggerFactory, statisticDirectory, statisticsFile);

    public static readonly
        Func<ILoggerFactory, TypedDirectoryPathString, TypedFileNameString, StatisticUtilities<StatisticModelEml>>
        EmlStatisticUtility = (loggerFactory, statisticDirectory, statisticsFile) =>
            new StatisticUtilities<StatisticModelEml>(loggerFactory, statisticDirectory, statisticsFile);
}

public class StatisticUtilities<TModel> where TModel : StatisticModel
{
    private readonly ILogger _logger;
    private readonly InterlockedCounter _entireDocuments;
    private readonly InterlockedCounter _failedDocuments;
    private readonly InterlockedCounter _changedDocuments;
    private readonly string _filePath;

    public StatisticUtilities(ILoggerFactory loggerFactory, TypedDirectoryPathString statisticsDirectory,
        TypedFileNameString statisticsFile)
    {
        _logger = loggerFactory.CreateLogger<StatisticUtilities<TModel>>();
        _entireDocuments = new InterlockedCounter();
        _failedDocuments = new InterlockedCounter();
        _changedDocuments = new InterlockedCounter();
        _filePath = $"{statisticsDirectory.Value}/{statisticsFile.Value}";
        _logger.LogInformation("initialize StatisticUtilities for type {TypeName}", typeof(TModel).Name);
        _checkAndCreateStatisticsDirectory(statisticsDirectory);
        _checkAndCreateStatisticsFile(_filePath);
    }

    private void _checkAndCreateStatisticsDirectory(TypedDirectoryPathString directoryPath)
    {
        _logger.LogInformation("check if directory {DirectoryPath} exists", directoryPath.Value);
        if (!Directory.Exists(directoryPath.Value))
            Directory.CreateDirectory(directoryPath.Value);
    }

    private void _checkAndCreateStatisticsFile(string filePath)
    {
        _logger.LogInformation("check if file {FilePath} exists", filePath);
        if (!File.Exists(filePath))
            File
                .Create(filePath)
                .Dispose();
    }

    public void AddToEntireDocuments() => _entireDocuments.Increment();
    public void AddToFailedDocuments() => _failedDocuments.Increment();
    public void AddToChangedDocuments(int value) => _changedDocuments.Add(value);

    public int EntireDocumentsCount() => _entireDocuments.Current();
    public int FailedDocumentsCount() => _failedDocuments.Current();
    public int ChangedDocumentsCount() => _changedDocuments.Current();

    public void AddJobStatisticToDatabase(ProcessingJobStatistic jobStatistic)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
        };
        var json = JsonSerializer.Serialize(jobStatistic, options);
        File.WriteAllText(_filePath, json);
    }
}