namespace DocSearchAIO.Classes
{
    public interface IProcessorType<out T>
    {
        string TypeAsString();
    }

    public class ProcessorTypeWord<T> : IProcessorType<T>
    {
        public string TypeAsString()
        {
            return GetType().Name;
        }
    }

    public class ProcessorTypePowerPoint<T> : IProcessorType<T>
    {
        public string TypeAsString()
        {
            return GetType().Name;
        }
    }

    public class ProcessorTypePdf<T> : IProcessorType<T>
    {
        public string TypeAsString()
        {
            return GetType().Name;
        }
    }
}