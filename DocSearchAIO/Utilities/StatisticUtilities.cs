using System.Text.Json;
using DocSearchAIO.Classes;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.Statistics;
using LanguageExt;

namespace DocSearchAIO.Utilities;

public static class StatisticUtilitiesProxy
{
    public static readonly Func<TypedDirectoryPathString,
            Seq<(IProcessorBase, Func<StatisticModel>)>>
        AsIEnumerable = (statisticsPath) => Seq<(IProcessorBase, Func<StatisticModel>)>()
            .Add((new ProcessorBaseWord(), () => new StatisticModelWord(statisticsPath)))
            .Add((new ProcessorBasePowerpoint(), () => new StatisticModelPowerpoint(statisticsPath)))
            .Add((new ProcessorBasePdf(), () => new StatisticModelPdf(statisticsPath)))
            .Add((new ProcessorBaseExcel(), () => new StatisticModelExcel(statisticsPath)))
            .Add((new ProcessorBaseMsg(), () => new StatisticModelMsg(statisticsPath)))
            .Add((new ProcessorBaseEml(), () => new StatisticModelEml(statisticsPath)));

    public static readonly Func<TypedDirectoryPathString, TypedFileNameString,
            StatisticUtilities<StatisticModelWord>>
        WordStatisticUtility = (statisticsDirectory, statisticsFile) =>
            new StatisticUtilities<StatisticModelWord>(statisticsDirectory, statisticsFile);

    public static readonly Func<TypedDirectoryPathString, TypedFileNameString,
            StatisticUtilities<StatisticModelPowerpoint>>
        PowerpointStatisticUtility = (statisticsDirectory, statisticsFile) =>
            new StatisticUtilities<StatisticModelPowerpoint>(statisticsDirectory, statisticsFile);

    public static readonly Func<TypedDirectoryPathString, TypedFileNameString,
            StatisticUtilities<StatisticModelPdf>>
        PdfStatisticUtility =
            (statisticsDirectory, statisticsFile) =>
                new StatisticUtilities<StatisticModelPdf>(statisticsDirectory, statisticsFile);

    public static readonly Func<TypedDirectoryPathString, TypedFileNameString,
            StatisticUtilities<StatisticModelExcel>>
        ExcelStatisticUtility = (statisticDirectory, statisticsFile) =>
            new StatisticUtilities<StatisticModelExcel>(statisticDirectory, statisticsFile);

    public static readonly
        Func<TypedDirectoryPathString, TypedFileNameString, StatisticUtilities<StatisticModelMsg>>
        MsgStatisticUtility = (statisticDirectory, statisticsFile) =>
            new StatisticUtilities<StatisticModelMsg>(statisticDirectory, statisticsFile);

    public static readonly
        Func<TypedDirectoryPathString, TypedFileNameString, StatisticUtilities<StatisticModelEml>>
        EmlStatisticUtility = (statisticDirectory, statisticsFile) =>
            new StatisticUtilities<StatisticModelEml>(statisticDirectory, statisticsFile);
}

public class StatisticUtilities<TModel> where TModel : StatisticModel
{
    private readonly ILogger _logger;
    private readonly InterlockedCounter _entireDocuments;
    private readonly InterlockedCounter _failedDocuments;
    private readonly InterlockedCounter _changedDocuments;
    private readonly string _filePath;

    public StatisticUtilities(TypedDirectoryPathString statisticsDirectory,
        TypedFileNameString statisticsFile)
    {
        _logger = LoggingFactoryBuilder.Build<StatisticUtilities<TModel>>();
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