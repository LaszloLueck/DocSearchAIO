using System;
using DocSearchAIO.Classes;

namespace DocSearchAIO.Scheduler
{
    public abstract class GenericSource
    {
    }

    public abstract class GenericSource<T> : GenericSource
    {
        public T Value { get; set; }
    }

    public class GenericSourceString : GenericSource<string>
    {
    }
    
    public class GenericSourceNullable<T> : GenericSource<T> where T : struct
    {

        private readonly T? _value;

        public GenericSourceNullable(T? value)
        {
            _value = value;
        }

        public T ValueOrDefault(T alternative)
        {
            return _value.GetValueOrDefault(alternative);
        }
        
    }

    public class GenericSourceFilePath : GenericSource
    {
        public readonly string Value;

        public GenericSourceFilePath(string value)
        {
            Value = value;
        }
    }
    


}