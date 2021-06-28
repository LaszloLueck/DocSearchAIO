using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Streams.Dsl;
using Akka.Util.Internal;
using CSharpFunctionalExtensions;

namespace DocSearchAIO.Utilities
{
    public static class CSharpFunctionalHelpers
    {
        
        public static async Task<IEnumerable<TResult>> WhenAll<TResult>(this IEnumerable<Task<TResult>> source)
        {
            return await Task.WhenAll(source);
        }

        public static void ForEach<TIn>(this IEnumerable<TIn> source, Action<TIn> action)
        {
            foreach (var value in source)
            {
                action.Invoke(value);
            }
        }

        public static void ForEach<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> kvList,
            Action<TKey, TValue> action) => kvList.ForEach(kv => action.Invoke(kv.Key, kv.Value));

        public static TOut IfTrueFalse<TOut>(this bool value,
            Func<TOut> falseAction,
            Func<TOut> trueAction)
        {
            return value ? trueAction.Invoke() : falseAction.Invoke();
        }

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

        public static Maybe<TOut> MaybeValue<TOut>(this TOut value)
        {
            return value == null ? Maybe<TOut>.None : Maybe<TOut>.From(value);
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> source) => source.ToDictionary(d => d.Key, d => d.Value);
        
        
        public static TOut ValueOr<TOut>(this TOut value, TOut alternative) =>
            value is null ? alternative is null ? default : alternative : value;

        public static Source<IEnumerable<TSource>, TMat> WithMaybeFilter<TSource, TMat>(
            this Source<IEnumerable<Maybe<TSource>>, TMat> source) => source.Select(Values);

        public static IEnumerable<TSource> Values<TSource>(this IEnumerable<Maybe<TSource>> source) =>
            source
                .Where(filtered => filtered.HasValue)
                .Select(selected => selected.Value);
    }
}