using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.Scheduler
{
    public class ComparersBase<TModel> where TModel : ElasticDocument
    {
        private readonly ConcurrentDictionary<string, ComparerObject> _comparerDictionary;
        private readonly ILogger _logger;
        private readonly string _compareFileDirectory;
        private readonly Comparers<TModel> _comparers;

        public ComparersBase(ILoggerFactory loggerFactory, ConfigurationObject configurationObject)
        {
            _logger = loggerFactory.CreateLogger<Comparers<TModel>>();
            _compareFileDirectory = $"{configurationObject.ComparerDirectory}/cmp_{typeof(TModel).Name}.cmp";
            _comparers = new Comparers<TModel>();
            _comparers.CheckAndCreateComparerDirectory(configurationObject.ComparerDirectory, _compareFileDirectory);
            _comparerDictionary =
                new ConcurrentDictionary<string, ComparerObject>(
                    _comparers.FillConcurrentDictionary(_logger, _compareFileDirectory));
        }

        public void RemoveComparerFile() => _comparers.RemoveComparerFile(_compareFileDirectory);

        public Task WriteAllLinesAsync() =>
            _comparers.WriteAllLinesAsync(_logger, _compareFileDirectory, _comparerDictionary);


        public Task<Maybe<TModel>> FilterExistingUnchanged(Maybe<TModel> model) =>
            _comparers.FilterExistingUnchanged(model, _comparerDictionary);
    }


    internal class Comparers<TModel> where TModel : ElasticDocument
    {
        internal readonly Action<string, string> CheckAndCreateComparerDirectory =
            (comparerFilePath, comparerFilePathAndName) =>
            {
                if (!Directory.Exists(comparerFilePath))
                    Directory.CreateDirectory(comparerFilePath);
                if (!File.Exists(comparerFilePathAndName))
                    File.Create(comparerFilePathAndName).Dispose();
            };

        internal readonly Func<ILogger, string, IEnumerable<KeyValuePair<string, ComparerObject>>>
            FillConcurrentDictionary =
                (logger, fileName) =>
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        return File
                            .ReadAllLines(fileName)
                            .AsParallel()
                            .WithDegreeOfParallelism(10)
                            .Select(line =>
                            {
                                var spl = line.Split(";");
                                if (spl.Length != 3) return Maybe<KeyValuePair<string, ComparerObject>>.None;
                                var cpo = new ComparerObject
                                    {DocumentHash = spl[0], PathHash = spl[1], OriginalPath = spl[2]};
                                return Maybe<KeyValuePair<string, ComparerObject>>.From(
                                    new KeyValuePair<string, ComparerObject>(cpo.PathHash, cpo));
                            })
                            .Values();
                    }
                    finally
                    {
                        sw.Stop();
                        logger.LogInformation($"FillConcurrentDictionary needs {sw.ElapsedMilliseconds} ms");
                    }
                };

        internal readonly Action<string> RemoveComparerFile = File.Delete;

        internal readonly Func<ILogger, string, ConcurrentDictionary<string, ComparerObject>, Task>
            WriteAllLinesAsync =
                async (logger, compareFileDirectory, comparerDictionary) =>
                {
                    logger.LogInformation($"write new comparer file in {compareFileDirectory}");
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        await File.WriteAllLinesAsync(compareFileDirectory,
                            comparerDictionary.Select(tpl =>
                                $"{tpl.Value.DocumentHash};{tpl.Value.PathHash};{tpl.Value.OriginalPath}"));
                    }
                    finally
                    {
                        sw.Stop();
                        logger.LogInformation($"WriteAllLinesAsync needs {sw.ElapsedMilliseconds} ms");
                    }
                };

        internal readonly Func<Maybe<TModel>, ConcurrentDictionary<string, ComparerObject>, Task<Maybe<TModel>>>
            FilterExistingUnchanged = async (document, comparerDictionary) =>
            {
                return await Task.Run(() =>
                {
                    var opt = document.Bind(doc =>
                    {
                        var contentHash = doc.ContentHash;
                        var pathHash = doc.Id;
                        var originalFilePath = doc.OriginalFilePath;
                        return comparerDictionary
                            .TryGetValue(pathHash, out var comparerObject)
                            .IfTrueFalse(
                                () =>
                                {
                                    var innerDoc = new ComparerObject
                                    {
                                        DocumentHash = contentHash,
                                        PathHash = pathHash,
                                        OriginalPath = originalFilePath
                                    };
                                    comparerDictionary.AddOrUpdate(pathHash, innerDoc, (_, _) => innerDoc);
                                    return Maybe<TModel>.From(doc);
                                },
                                () =>
                                {
                                    if (comparerObject == null) return Maybe<TModel>.None;

                                    if (comparerObject.DocumentHash == contentHash)
                                        return Maybe<TModel>.None;

                                    comparerObject.DocumentHash = contentHash;
                                    comparerDictionary.AddOrUpdate(pathHash, comparerObject,
                                        (_, _) => comparerObject);
                                    return Maybe<TModel>.From(doc);
                                });
                    });
                    return opt;
                });
            };
    }
}