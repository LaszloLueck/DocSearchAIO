using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Akka.Streams.Dsl;
using Akka.Util.Internal;
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using DocumentFormat.OpenXml;

namespace DocSearchAIO.Scheduler
{
    public static class StaticHelpers
    {
        public static void IfTrue(this bool value, Action action)
        {
            if (value)
                action.Invoke();
        }

        public static void Map<TIn>(this Maybe<TIn> source, Action<TIn> processor)
        {
            if (source.HasValue)
                processor.Invoke(source.Value);
        }

        public static Maybe<TOut> MaybeValue<TOut>(this TOut value)
        {
            return value == null ? Maybe<TOut>.None : Maybe<TOut>.From(value);
        }

        public static void IfTrueFalse(this bool value, Action falseAction, Action trueAction)
        {
            if (value)
            {
                trueAction.Invoke();
            }
            else
            {
                falseAction.Invoke();
            }
        }

        public static IEnumerable<Type> GetSubtypesOfType<TIn>()
            =>
                from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                from assemblyType in domainAssembly.GetTypes()
                where typeof(TIn).IsAssignableFrom(assemblyType)
                where assemblyType.IsSubclassOf(typeof(TIn))
                select assemblyType;

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> source) => source.ToDictionary(d => d.Key, d => d.Value);

        public static void IfTrueFalse<TInputLeft, TInputRight>(this bool value, (TInputLeft, TInputRight) parameters,
            Action<TInputLeft> falseAction,
            Action<TInputRight> trueAction)
        {
            var (inputLeft, inputRight) = parameters;
            if (value)
            {
                trueAction.Invoke(inputRight);
            }
            else
            {
                falseAction.Invoke(inputLeft);
            }
        }

        public static TOut IfTrueFalse<TOut>(this bool value,
            Func<TOut> falseAction,
            Func<TOut> trueAction)
        {
            return value ? trueAction.Invoke() : falseAction.Invoke();
        }

        public static string GetTypeNameFromProcessorBase<TIn>(this TIn processorBase) where TIn : ProcessorBase
        {
            return processorBase.GetDerivedModelName;
        }

        public static TOut DirectoryNotExistsAction<TIn, TOut>(this TIn path, Func<TIn, TOut> action)
            where TOut : GenericSourceString
            where TIn : TOut
        {
            return !Directory.Exists(path.Value) ? action.Invoke(path) : path;
        }

        public static void DictionaryKeyExistsAction<TDicKey, TDicValue>(
            this Dictionary<TDicKey, TDicValue> source, TDicKey comparer,
            Action<KeyValuePair<TDicKey, TDicValue>> action)
        {
            if (source.ContainsKey(comparer))
                action.Invoke(new KeyValuePair<TDicKey, TDicValue>(comparer, source[comparer]));
        }

        public static void AndThen<TIn>(this TIn source, Action<TIn> action) where TIn : GenericSource =>
            action.Invoke(source);

        public static GenericSourceString AsGenericSourceString(this string value) => new() { Value = value };


        public static Source<IEnumerable<TSource>, TMat> WithMaybeFilter<TSource, TMat>(
            this Source<IEnumerable<Maybe<TSource>>, TMat> source) => source.Select(Values);

        public static IEnumerable<TSource> Values<TSource>(this IEnumerable<Maybe<TSource>> source) =>
            source
                .Where(filtered => filtered.HasValue)
                .Select(selected => selected.Value);
        
        public static async Task<string> CreateMd5HashString(string stringValue)
        {
            return await Task.Run(async () =>
            {
                using var md5 = MD5.Create();
                await using var ms = new MemoryStream();
                await using var wt = new StreamWriter(ms);
                await wt.WriteAsync(stringValue);
                await wt.FlushAsync();
                ms.Position = 0;
                return BitConverter.ToString(await md5.ComputeHashAsync(ms));
            });
        }
        

        public static Source<TSource, TMat> CountEntireDocs<TSource, TMat, TModel>(this Source<TSource, TMat> source,
            StatisticUtilities<TModel> statisticUtilities) where TModel : StatisticModel
        {
            return source.Select(t =>
            {
                statisticUtilities.AddToEntireDocuments();
                return t;
            });
        }

        public static string JoinString(this IEnumerable<string> source, string separator) =>
            string.Join(separator, source); 
        
        public static Source<IEnumerable<TSource>, TMat> CountFilteredDocs<TSource, TMat, TModel>(
            this Source<IEnumerable<TSource>, TMat> source,
            StatisticUtilities<TModel> statisticUtilities) where TModel : StatisticModel
        {
            return source.Select(e =>
            {
                var cntArr = e.ToArray();
                statisticUtilities.AddToChangedDocuments(cntArr.Length);
                return cntArr.AsEnumerable();
            });
        }

        public static readonly Action<IEnumerable<OpenXmlElement>, StringBuilder> ExtractTextFromElement = (list, sb) =>
        {
            list
                .ForEach(element =>
                {
                    switch (element.LocalName)
                    {
                        case "t" when !element.ChildElements.Any():
                            sb.Append(element.InnerText);
                            break;
                        case "p":
                            ExtractTextFromElement(element.ChildElements, sb);
                            sb.Append(' ');
                            break;
                        case "br":
                            sb.Append(' ');
                            break;
                        default:
                            ExtractTextFromElement(element.ChildElements, sb);
                            break;
                    }
                });
        };

        public static readonly Func<string, IList<(string, string)>, string> ReplaceSpecialStrings = (input, list) =>
        {
            while (true)
            {
                if (!list.Any()) return input;

                input = Regex.Replace(input, list[0].Item1, list[0].Item2);
                list.RemoveAt(0);
            }
        };
    }
}