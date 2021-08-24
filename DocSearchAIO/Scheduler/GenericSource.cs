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

    public class TypedEncryptedString : GenericSourceString
    {
        public TypedEncryptedString(string value) : base(value)
        {
        }
    }

    public class TypedEncryptedInputString : GenericSourceString
    {
        public TypedEncryptedInputString(string value) : base(value)
        {
        }
    }

}