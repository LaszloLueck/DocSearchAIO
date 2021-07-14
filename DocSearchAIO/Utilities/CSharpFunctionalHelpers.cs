using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using Akka.Streams.Dsl;
using CSharpFunctionalExtensions;

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

        [Pure]
        public static TOut IfTrueFalse<TOut>(this bool value,
            Func<TOut> falseAction,
            Func<TOut> trueAction) => value ? trueAction.Invoke() : falseAction.Invoke();

        public static void IfTrue(this bool value, Action action)
        {
            if (value)
                action.Invoke();
        }

        public static void DictionaryKeyExistsAction<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey comparer,
            Action<TKey, TValue> action)
        {
            if (!source.ContainsKey(comparer)) return;
            action.Invoke(comparer, source[comparer]);
        }

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

        public static void Map<TIn>(this Maybe<TIn> source, Action<TIn> processor)
        {
            if (source.HasValue)
                processor.Invoke(source.Value);
        }

        // public static Maybe<TOut> Map<TIn, TOut>(this Maybe<TIn> source, Func<TIn, TOut> processor)
        // {
        //     return source.HasValue ? Maybe.From(processor.Invoke(source.Value)) : Maybe<TOut>.None;
        // }
        
        [Pure]
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> source) where TKey : notnull =>
            source.ToDictionary(d => d.Key, d => d.Value);

        [Pure]
        public static TOut ResolveNullable<TIn, TOut>([AllowNull] this TIn? nullable, [DisallowNull][NotNull] TOut alternative,
            [NotNull] Func<TIn, TOut, TOut> action) => nullable is not null ? action.Invoke(nullable, alternative) : alternative;

        [Pure]
        public static TOut ResolveNullable<TIn, TOut>([AllowNull] this TIn? nullable, [DisallowNull, NotNull] TOut alternative,
            [NotNull] Func<TIn, TOut, TOut> some, [NotNull] Func<TOut, TOut> none) =>
            nullable is not null ? some.Invoke(nullable, alternative) : none.Invoke(alternative);

        [Pure]
        public static Source<IEnumerable<TSource>, TMat> WithMaybeFilter<TSource, TMat>(
            [NotNull] this Source<IEnumerable<Maybe<TSource>>, TMat> source) => source.Select(Values);

        [Pure]
        public static IEnumerable<TOut> SelectKv<TKey, TValue, TOut>([NotNull] this IEnumerable<KeyValuePair<TKey, TValue>> dic,
            [NotNull] Func<TKey, TValue, TOut> action) => dic.Select(kv => action.Invoke(kv.Key, kv.Value));
        
        [Pure]
        public static IEnumerable<KeyValuePair<TKey, TValue>> Where<TKey, TValue>([NotNull] this IEnumerable<KeyValuePair<TKey, TValue>> source,
            [NotNull] Func<TKey, TValue, bool> action) => source.Where(kv => action.Invoke(kv.Key, kv.Value));

        [Pure]
        public static IEnumerable<TSource> Values<TSource>([NotNull, DisallowNull] this IEnumerable<Maybe<TSource>> source) =>
            source
                .Where(filtered => filtered.HasValue)
                .Select(selected => selected.Value);
    }
}