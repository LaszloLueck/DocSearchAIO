using System.Collections.Concurrent;
using System.Diagnostics;
using DocSearchAIO.Utilities;
using LanguageExt;
using MethodTimer;

namespace DocSearchAIO.Classes;

public abstract class ComparerModel
{
    private readonly ILogger? _logger;

    protected abstract string DerivedModelName { get; }

    private readonly string _comparerDirectory;

    private string ComparerFileName => $"cmp_{DerivedModelName}.cmp";

    public string ComparerFilePath => $"{_comparerDirectory}/{ComparerFileName}";

    private readonly ConcurrentDictionary<string, ComparerObject> _comparerObjects = new();

    protected ComparerModel(ILoggerFactory loggerFactory, string comparerDirectory)
    {
        _logger = loggerFactory.CreateLogger<ComparerModel>();
        _comparerDirectory = comparerDirectory;
        var cd = Convert(ComparerHelper
            .FillConcurrentDictionary(ComparerFilePath, _logger).ToDictionary());
        CheckAndCreateComparerDirectory();
        _comparerObjects = cd;
    }

    private static readonly
        Func<IReadOnlyDictionary<string, ComparerObject>, ConcurrentDictionary<string, ComparerObject>>
        Convert = source => new ConcurrentDictionary<string, ComparerObject>(source);

    protected ComparerModel(string comparerDirectory)
    {
        _comparerDirectory = comparerDirectory;
    }

    public void CleanDictionaryAndRemoveComparerFile()
    {
        _logger?.LogInformation("cleanup comparer dictionary before removing the file");
        _comparerObjects.Clear();
        RemoveComparerFile();
        CheckAndCreateComparerDirectory();
    }

    public void RemoveComparerFile()
    {
        _logger?.LogInformation("remove comparer file {GetComparerFilePath} for key {DerivedModelName}",
            ComparerFilePath, DerivedModelName);
        ComparerHelper.RemoveComparerFile(ComparerFilePath);
    }

    [Time]
    public async Task WriteAllLinesAsync()
    {
        _logger?.LogInformation("write new comparer file in {ComparerDirectory}", _comparerDirectory);
        await ComparerHelper.WriteAllLinesAsync(_comparerObjects, ComparerFilePath);
    }

    private void CheckAndCreateComparerDirectory()
    {
        _logger?.LogInformation("check if directory {ComparerDirectory} exists", _comparerDirectory);
        if (!ComparerHelper.CheckIfDirectoryExists(_comparerDirectory))
            ComparerHelper.CreateDirectory(_comparerDirectory);
        _logger?.LogInformation("check if comparer file {GetComparerFilePath} exists", ComparerFilePath);
        if (!ComparerHelper.CheckIfFileExists(ComparerFilePath))
            ComparerHelper.CreateComparerFile(ComparerFilePath);
    }

    public async Task<Option<TModel>> FilterExistingUnchanged<TModel>(Option<TModel> document)
        where TModel : ElasticDocument
    {
        return await Task.Run(() =>
            document.Bind(doc =>
            {
                var contentHash = doc.ContentHash;
                var pathHash = doc.Id;
                var originalFilePath = doc.OriginalFilePath;


                return _comparerObjects
                    .TryGetValue(pathHash, out var comparerObject)
                    .IfTrueFalse(
                        () =>
                        {
                            var innerDoc = new ComparerObject(pathHash, contentHash, originalFilePath);
                            _comparerObjects.AddOrUpdate(pathHash, innerDoc, (_, _) => innerDoc);
                            return doc;
                        },
                        () =>
                        {
                            if (comparerObject == null) return Option<TModel>.None;

                            if (comparerObject.DocumentHash == contentHash)
                                return Option<TModel>.None;
                            var comparerObjectCopy = new ComparerObject(comparerObject.PathHash, contentHash,
                                comparerObject.OriginalPath);
                            _comparerObjects.AddOrUpdate(pathHash, comparerObjectCopy,
                                (_, _) => comparerObjectCopy);
                            return doc;
                        });
            })
        );
    }
}

public sealed class ComparerModelWord : ComparerModel
{
    protected override string DerivedModelName => GetType().Name;

    public ComparerModelWord(string comparerDirectory) : base(comparerDirectory)
    {
    }

    public ComparerModelWord(ILoggerFactory loggerFactory, string comparerDirectory) : base(loggerFactory,
        comparerDirectory)
    {
    }
}

public sealed class ComparerModelPowerpoint : ComparerModel
{
    protected override string DerivedModelName => GetType().Name;

    public ComparerModelPowerpoint(string comparerDirectory) : base(comparerDirectory)
    {
    }

    public ComparerModelPowerpoint(ILoggerFactory loggerFactory, string comparerDirectory) : base(loggerFactory,
        comparerDirectory)
    {
    }
}

public sealed class ComparerModelPdf : ComparerModel
{
    protected override string DerivedModelName => GetType().Name;

    public ComparerModelPdf(string comparerDirectory) : base(comparerDirectory)
    {
    }

    public ComparerModelPdf(ILoggerFactory loggerFactory, string comparerDirectory) : base(loggerFactory,
        comparerDirectory)
    {
    }
}

public sealed class ComparerModelExcel : ComparerModel
{
    protected override string DerivedModelName => GetType().Name;

    public ComparerModelExcel(string comparerDirectory) : base(comparerDirectory)
    {
    }

    public ComparerModelExcel(ILoggerFactory loggerFactory, string comparerDirectory) : base(loggerFactory,
        comparerDirectory)
    {
    }
}

public sealed class ComparerModelMsg : ComparerModel
{
    protected override string DerivedModelName => GetType().Name;

    public ComparerModelMsg(string comparerDirectory) : base(comparerDirectory)
    {
    }

    public ComparerModelMsg(ILoggerFactory loggerFactory, string comparerDirectory) : base(loggerFactory,
        comparerDirectory)
    {
    }
}

public sealed class ComparerModelEml : ComparerModel
{
    public ComparerModelEml(ILoggerFactory loggerFactory, string comparerDirectory) : base(loggerFactory,
        comparerDirectory)
    {
    }

    public ComparerModelEml(string comparerDirectory) : base(comparerDirectory)
    {
    }

    protected override string DerivedModelName => GetType().Name;
}