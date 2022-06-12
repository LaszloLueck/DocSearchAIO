using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Akka.Streams.Dsl;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;

namespace DocSearchAIO.Utilities;

public static class CSharpFunctionalHelpers
{
    public static void ForEach<TIn>(this IEnumerable<TIn> source, Action<TIn> action)
    {
        foreach (var value in source)
        {
            action.Invoke(value);
        }
    }
    
    public static Option<T> TryFirst<T>(this IEnumerable<T> source)
    {
        return source.Any() ? source.First() : Option<T>.None;
    }

    public static Option<T> TryFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        var firstOrEmpty = source.Where(predicate).Take(1).ToList();
        if (firstOrEmpty.Any())
        {
            return firstOrEmpty[0];
        }

        return Option<T>.None;
    }
    

    public static T GetValueOrDefault<T>(this Option<T> source, T alternative)
    {
        return source.IsNone ? alternative : source.ValueUnsafe();
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
    public static T OrElseIfNull<T>(this T? nullable, [DisallowNull, NotNull] T alternative) =>
        nullable is not null ? nullable! : alternative;

    [Pure]
    public static Source<IEnumerable<TSource>, TMat> WithMaybeFilter<TSource, TMat>(
        this Source<IEnumerable<Option<TSource>>, TMat> source) => source.Select(d => d.Somes());

    [Pure]
    public static IEnumerable<TOut> SelectKv<TKey, TValue, TOut>(this IEnumerable<KeyValuePair<TKey, TValue>> dic,
        Func<TKey, TValue, TOut> action) => dic.Select(kv => action.Invoke(kv.Key, kv.Value));

    [Pure]
    public static IEnumerable<TOut> SelectTuple<TKey, TValue, TOut>(
        this IEnumerable<Tuple<TKey, TValue>> source, Func<TKey, TValue, TOut> action) =>
        source.Select(tuple => action.Invoke(tuple.Item1, tuple.Item2));
    
}