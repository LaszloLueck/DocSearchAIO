using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Akka;
using Akka.Streams.Dsl;
using CSharpFunctionalExtensions;
using DocSearchAIO.Utilities;

namespace DocSearchAIO.Classes
{
    public static class ComparerHelper
    {
        public static readonly Func<string, Source<ComparerObject, NotUsed>> GetComparerObjectSource = path => File
            .ReadAllLines(path)
            .AsParallel()
            .WithDegreeOfParallelism(10)
            .Select(ConvertLine!)
            .Values()
            .AsAkkaSource();

        private static Source<TIn, NotUsed> AsAkkaSource<TIn>(this IEnumerable<TIn> ieNumerable)
        {
            return Source.From(ieNumerable);
        }

        [NotNull]
        private static readonly Func<string, Maybe<ComparerObject>> ConvertLine = line =>
        {
            var spl = line.Split(";");
            if (spl.Length != 3) return Maybe<ComparerObject>.None;
            var cpo = new ComparerObject(spl[1], spl[0], spl[2]);
            return cpo;
        };

        public static readonly Func<string, ConcurrentDictionary<string, ComparerObject>> FillConcurrentDictionary =
            path =>
            {
                var retDictionary = File
                    .ReadAllLines(path)
                    .AsParallel()
                    .WithDegreeOfParallelism(10)
                    .Select(ConvertLine)
                    .Values()
                    .Select(cpo => KeyValuePair.Create(cpo.PathHash, cpo))
                    .ToDictionary();
                return new ConcurrentDictionary<string, ComparerObject>(retDictionary);
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
}