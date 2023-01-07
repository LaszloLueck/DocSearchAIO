namespace DocSearchAIO.Classes;

public interface IProcessorBase
{
    public string DerivedModelName { get; }
    public string ShortName { get; }
}

public class ProcessorBaseWord : IProcessorBase
{
    public string DerivedModelName => GetType().Name;
    public string ShortName => "Word";
}

public class ProcessorBasePowerpoint : IProcessorBase
{
    public string DerivedModelName => GetType().Name;
    public string ShortName => "Powerpoint";
}

public class ProcessorBasePdf : IProcessorBase
{
    public string DerivedModelName => GetType().Name;
    public string ShortName => "Pdf";
}

public class ProcessorBaseExcel : IProcessorBase
{
    public string DerivedModelName => GetType().Name;
    public string ShortName => "Excel";
}

public class ProcessorBaseMsg : IProcessorBase
{
    public string DerivedModelName => GetType().Name;

    public string ShortName => "Msg";
}

public class ProcessorBaseEml : IProcessorBase
{
    public string DerivedModelName => GetType().Name;

    public string ShortName => "Eml";
}