using System;
using System.Collections.Generic;
using System.IO;
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

        public static void Either<TInputLeft, TInputRight>(this bool source, (TInputLeft, TInputRight) parameters, Action<TInputLeft> left,
            Action<TInputRight> right)
        {
            var (inputLeft, inputRight) = parameters;
            if (source)
            {
                right.Invoke(inputRight);
            }
            else
            {
                left.Invoke(inputLeft);
            }
        }

        public static void DirectoryNotExistsAction(this string path, Action<string> action)
        {
            if (!Directory.Exists(path))
                action.Invoke(path);
        }

        public static Source<IEnumerable<TSource>, TMat> WithOptionFilter<TSource, TMat>(this Source<IEnumerable<Option<TSource>>, TMat> source)
        {
            return source.Select(d => d.Values());
        }
    }
}