namespace DocSearchAIO.Classes
{
    public record CleanupDocument;

    public abstract record WordCleanupDocument : CleanupDocument;

    public abstract record PowerpointCleanupDocument : CleanupDocument;

    public abstract record ExcelCleanupDocument : CleanupDocument;

    public abstract record PdfCleanupDocument : CleanupDocument;
}