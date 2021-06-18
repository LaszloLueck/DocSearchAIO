namespace DocSearchAIO.Classes
{

    public interface IProcessorType
    {
        string TypeAsString();
        string ShortName();
        string Description();
    }

    public class ProcessorTypeWord : IProcessorType
    {
        public string TypeAsString()
        {
            return GetType().Name;
        }

        public string ShortName()
        {
            return "Word";
        }

        public string Description()
        {
            return "Internal container for identifying a word processor";
        }
    }

    public class ProcessorTypePowerPoint : IProcessorType
    {
        public string TypeAsString()
        {
            return GetType().Name;
        }

        public string ShortName()
        {
            return "Powerpoint";
        }

        public string Description()
        {
            return "Internal container for identifying a powerpoint processor";
        }
    }

    public class ProcessorTypePdf : IProcessorType
    {
        public string TypeAsString()
        {
            return GetType().Name;
        }

        public string ShortName()
        {
            return "Pdf";
        }

        public string Description()
        {
            return "Internal container for identifying a pdf processor";
        }
    }
}