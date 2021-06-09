using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Akka.Streams.Dsl;
using CSharpFunctionalExtensions;

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

        public static void AndThen<TIn>(this TIn source, Action<TIn> action) where TIn : GenericSource => action.Invoke(source);

        public static GenericSourceString AsGenericSourceString(this string value) => new() { Value = value };

        public static Source<IEnumerable<TSource>, TMat> WithMaybeFilter<TSource, TMat>(
            this Source<IEnumerable<Maybe<TSource>>, TMat> source) => source.Select(Values);

        private static IEnumerable<TSource> Values<TSource>(this IEnumerable<Maybe<TSource>> source) =>
            source
                .Where(filtered => filtered.HasValue)
                .Select(selected => selected.Value);
    }
}