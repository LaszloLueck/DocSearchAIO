using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Akka.Streams.Dsl;
using LanguageExt;

namespace DocSearchAIO.Utilities;

public static class CSharpFunctionalHelpers
{

    [Pure]
    public static T IfNull<T>(this T? self, [NotNull, DisallowNull] T noneValue) => (self.IsNull() ? noneValue : self)!;
    
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
    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
        this IEnumerable<ValueTuple<TKey, TValue>> source) where TKey : notnull =>
        source.ToDictionary(d => d.Item1, d => d.Item2);
    
    [Pure]
    public static TOut ResolveNullable<TIn, TOut>(this TIn? nullable, [DisallowNull] [NotNull] TOut alternative,
        Func<TIn, TOut, TOut> action) => nullable is not null ? action.Invoke(nullable, alternative) : alternative;
    

    [Pure]
    public static Source<IEnumerable<TSource>, TMat> WithMaybeFilter<TSource, TMat>(
        this Source<IEnumerable<Option<TSource>>, TMat> source) => source.Select(d => d.Somes());

}