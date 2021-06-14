using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Akka.Streams.Dsl;
using CSharpFunctionalExtensions;
using Quartz;

namespace DocSearchAIO.Scheduler
{
    public static class StaticHelpers
    {
        public static void IfTrue(this bool value, Action action)
        {
            if (value)
                action.Invoke();
        }

        public static void MaybeTrue<TIn>(this Maybe<TIn> source, Action<TIn> processor)
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

        public static IEnumerable<KeyValuePair<TOutKey, TOut>> TransformGenericPartial<TOut, TOutKey, TInKey, TValue>(
            this IEnumerable<KeyValuePair<TInKey, TValue>> dic,
            Func<KeyValuePair<TInKey, TValue>, KeyValuePair<TOutKey, TOut>> action)
        {
            return dic.Select(action.Invoke);
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

        public static GenericSourceString AsGenericSourceString(this string value) => new() {Value = value};


        public static Source<IEnumerable<TSource>, TMat> WithMaybeFilter<TSource, TMat>(
            this Source<IEnumerable<Maybe<TSource>>, TMat> source) => source.Select(Values);

        public static IEnumerable<TSource> Values<TSource>(this IEnumerable<Maybe<TSource>> source) =>
            source
                .Where(filtered => filtered.HasValue)
                .Select(selected => selected.Value);

        public static Source<TSource, TMat> CountEntireDocs<TSource, TMat>(this Source<TSource, TMat> source,
            StatisticUtilities statisticUtilities)
        {
            return source.Select(t =>
            {
                statisticUtilities.AddToEntireDocuments();
                return t;
            });
        }

        public static Source<IEnumerable<TSource>, TMat> CountFilteredDocs<TSource, TMat>(
            this Source<IEnumerable<TSource>, TMat> source,
            StatisticUtilities statisticUtilities)
        {
            return source.Select(e =>
            {
                var cntArr = e.ToArray();
                statisticUtilities.AddToChangedDocuments(cntArr.Length);
                return cntArr.AsEnumerable();
            });
        }
    }
}