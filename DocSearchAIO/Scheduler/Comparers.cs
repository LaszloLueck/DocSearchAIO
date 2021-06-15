using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.Scheduler
{
    public class Comparers<TModel> where TModel : ElasticDocument
    {
        private readonly ConcurrentDictionary<string, ComparerObject> _comparerDictionary;
        private readonly ILogger _logger;
        private readonly string _compareFileDirectory;

        public Comparers(ILoggerFactory loggerFactory, ConfigurationObject configurationObject)
        {
            _logger = loggerFactory.CreateLogger<Comparers<TModel>>();
            _compareFileDirectory = $"{configurationObject.ComparerDirectory}/cmp_{typeof(TModel).Name}.cmp";
            if (!Directory.Exists(configurationObject.ComparerDirectory))
                Directory.CreateDirectory(configurationObject.ComparerDirectory);
            if (!File.Exists(_compareFileDirectory))
                File.Create(_compareFileDirectory).Dispose();
            
            _comparerDictionary =
                new ConcurrentDictionary<string, ComparerObject>(FillConcurrentDictionary(_compareFileDirectory));
        }

        private static IEnumerable<KeyValuePair<string, ComparerObject>> FillConcurrentDictionary(string fileName)
        {
            return File.ReadAllLines(fileName).Select(str =>
            {
                var spl = str.Split(";");
                var cpo = new ComparerObject {DocumentHash = spl[0], PathHash = spl[1], OriginalPath = spl[2]};
                return new KeyValuePair<string, ComparerObject>(cpo.PathHash, cpo);
            });
        }

        public void RemoveComparerFile() => File.Delete(_compareFileDirectory);

        public async Task WriteAllLinesAsync()
        {
            _logger.LogInformation("write new comparer file in {ComparerDirectory}", _compareFileDirectory);
            await File.WriteAllLinesAsync(_compareFileDirectory,
                _comparerDictionary.Select(tpl =>
                    $"{tpl.Value.DocumentHash};{tpl.Value.PathHash};{tpl.Value.OriginalPath}"));
        }

        public async Task<Maybe<TModel>> FilterExistingUnchanged(Maybe<TModel> document)
        {
            return await Task.Run(() =>
            {
                var opt = document.Bind(doc =>
                {
                    var contentHash = doc.ContentHash;
                    var pathHash = doc.Id;
                    var originalFilePath = doc.OriginalFilePath;
                    return _comparerDictionary
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
                                _comparerDictionary.AddOrUpdate(pathHash, innerDoc, (_, _) => innerDoc);
                                return Maybe<TModel>.From(doc);
                            },
                            () =>
                            {
                                if (comparerObject == null) return Maybe<TModel>.None;

                                if (comparerObject.DocumentHash == contentHash)
                                    return Maybe<TModel>.None;


                                comparerObject.DocumentHash = contentHash;
                                _comparerDictionary.AddOrUpdate(pathHash, comparerObject, (_, _) => comparerObject);
                                return Maybe<TModel>.From(doc);
                            });
                });
                return opt;
            });
        }
    }
}