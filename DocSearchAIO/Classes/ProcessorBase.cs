namespace DocSearchAIO.Classes
{

    public abstract class ProcessorBase
    {
        public abstract string GetDerivedModelName { get; }
        public abstract string ShortName { get; }
    }
    
    public class ProcessorBaseWord : ProcessorBase
    {
        public override string GetDerivedModelName => GetType().Name;
        public override string ShortName => "Word";
    }

    public class ProcessorBasePowerpoint : ProcessorBase
    {
        public override string GetDerivedModelName => GetType().Name;
        public override string ShortName => "Powerpoint";
    }

    public class ProcessorBasePdf : ProcessorBase
    {
        public override string GetDerivedModelName => GetType().Name;
        public override string ShortName => "Pdf";
    }

    public class ProcessorBaseExcel : ProcessorBase
    {
        public override string GetDerivedModelName => GetType().Name;
        public override string ShortName => "Excel";
    }
}