using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Akka.Streams.Dsl;
using CSharpFunctionalExtensions;
using DocSearchAIO.DocSearch.TOs;
using DocumentFormat.OpenXml.Office2010.Drawing;

namespace DocSearchAIO.Utilities
{
    public static class CSharpFunctionalHelpers
    {
        [Pure]
        public static async Task<IEnumerable<TResult>> WhenAll<TResult>(this IEnumerable<Task<TResult>> source) =>
            await Task.WhenAll(source);

        public static void ForEach<TIn>(this IEnumerable<TIn> source, Action<TIn> action)
        {
            foreach (var value in source)
            {
                action.Invoke(value);
            }
        }

        public static void ForEach<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> kvList,
            Action<TKey, TValue> action) => kvList.ForEach(kv => action.Invoke(kv.Key, kv.Value));

        public static void ForEach<TTuple1, TTuple2>(this IEnumerable<(TTuple1, TTuple2)> source,
            Action<TTuple1, TTuple2> action)
        {
            foreach (var valueTuple in source)
            {
                action.Invoke(valueTuple.Item1, valueTuple.Item2);
            }
        }

        [Pure]
        public static TOut IfTrueFalse<TOut>(this bool value,
            Func<TOut> falseAction,
            Func<TOut> trueAction) => value ? trueAction.Invoke() : falseAction.Invoke();

        public static void DictionaryKeyExistsAction<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey comparer,
            Action<TKey, TValue> action)
        {
            if (!source.ContainsKey(comparer)) return;
            action.Invoke(comparer, source[comparer]);
        }

        [Pure]
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> source) where TKey : notnull =>
            source.ToDictionary(d => d.Key, d => d.Value);

        [Pure]
        public static TOut ResolveNullable<TIn, TOut>(this TIn? nullable, [DisallowNull] [NotNull] TOut alternative,
            Func<TIn, TOut, TOut> action) => nullable is not null ? action.Invoke(nullable, alternative) : alternative;

        [Pure]
        public static Source<IEnumerable<TSource>, TMat> WithMaybeFilter<TSource, TMat>(
            this Source<IEnumerable<Maybe<TSource>>, TMat> source) => source.Select(Values);

        [Pure]
        public static IEnumerable<TOut> SelectKv<TKey, TValue, TOut>(this IEnumerable<KeyValuePair<TKey, TValue>> dic,
            Func<TKey, TValue, TOut> action) => dic.Select(kv => action.Invoke(kv.Key, kv.Value));

        [Pure]
        public static IEnumerable<TOut> SelectTuple<TKey, TValue, TOut>(
            this IEnumerable<Tuple<TKey, TValue>> source, Func<TKey, TValue, TOut> action) =>
            source.Select(tuple => action.Invoke(tuple.Item1, tuple.Item2));

        [Pure]
        public static IEnumerable<TSource> Values<TSource>(this IEnumerable<Maybe<TSource>> source) =>
            source
                .Where(filtered => filtered.HasValue)
                .Select(selected => selected.Value);
    }
}