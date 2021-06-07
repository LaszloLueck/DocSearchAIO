using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Akka.Streams.Dsl;
using Optional;
using Optional.Collections;

namespace DocSearchAIO.Scheduler
{
    public static class Helpers
    {
        public static Option<TOut> AsOptionalValue<TOut>(this bool source, Func<TOut> action)
        {
            return source.SomeWhen(t => t).Map(_ => action.Invoke());
        }

        public static Option<TOut> IfTrue<TIn, TOut>(this bool value, TIn input, Func<TIn, TOut> action)
        {
            return value ? Option.Some(action.Invoke(input)) : Option.None<TOut>();
        }
        
        public static void IfTrue<TIn>(this bool value, TIn input, Action<TIn> action)
        {
            if (value)
                action.Invoke(input);
        }

        public static void IfTrue(this bool value, Action action)
        {
            if(value)
                action.Invoke();
        }

        public static void IfTrueFalse(this bool value, Action falseAction, Action trueAction)
        {
            if(value)
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

        public static void AndThen<TIn>(this TIn source, Action<TIn> action)
        {
            action.Invoke(source);
        }

        public static TOut AndThen<TIn, TOut>(this TIn source, Func<TIn, TOut> action)
        {
            return action.Invoke(source);
        }

        public static GenericSourceString AsGenericSourceString(this string value) => new() {Value = value};

        public static Source<IEnumerable<TSource>, TMat> WithOptionFilter<TSource, TMat>(
            this Source<IEnumerable<Option<TSource>>, TMat> source)
        {
            return source.Select(d => d.Values());
        }
    }

    public class GenericSourceString : GenericSource<string>
    {
    }

    public abstract class GenericSource<T>
    {
        public T Value { get; set; }
    }
}