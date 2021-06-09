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
}