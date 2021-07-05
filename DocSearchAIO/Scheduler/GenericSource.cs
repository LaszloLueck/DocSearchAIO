namespace DocSearchAIO.Scheduler
{
    public abstract class GenericSource
    {
    }

    public abstract class GenericSource<T> : GenericSource
    {
        public readonly T Value;

        protected GenericSource(T value)
        {
            Value = value;
        }
    }

    public abstract class GenericSourceString : GenericSource
    {
        public readonly string Value;

        protected GenericSourceString(string value)
        {
            Value = value;
        }
        
        public override string ToString()
        {
            return Value;
        }
    }

    public class GenericSourceNullable<T> : GenericSource where T : struct
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
    
    public class TypedGroupNameString : GenericSourceString
    {
        public TypedGroupNameString(string value) : base(value)
        {
        }
    }

    public class TypedDirectoryPathString : GenericSourceString
    {
        public TypedDirectoryPathString(string value) : base(value)
        {
        }
    }

    public class TypedFileNameString : GenericSourceString
    {
        public TypedFileNameString(string value) : base(value)
        {
        }
    }

    public class TypedFilePathString : GenericSourceString
    {
        public TypedFilePathString(string value) : base(value)
        {
        }
    }
    
    public class TypedCommentString : GenericSourceString
    {
        public TypedCommentString(string value) : base(value)
        {
        }
    }

    public class TypedContentString : GenericSourceString
    {
        public TypedContentString(string value) : base(value)
        {
        }
    }

    public class TypedSuggestString : GenericSourceString
    {
        public TypedSuggestString(string value) : base(value)
        {
        }
    }

    public class TypedMd5String : GenericSourceString
    {
        public TypedMd5String(string value) : base(value)
        {
        }
    }

    public class TypedMd5InputString : GenericSourceString
    {
        public TypedMd5InputString(string value) : base(value)
        {
        }
    }

}