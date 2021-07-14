namespace DocSearchAIO.DocSearch.TOs
{
    public record OptionDialogRequest(bool FilterWord, bool FilterExcel, bool FilterPowerpoint, bool FilterPdf,
        bool WordIndexExists, bool ExcelIndexExists, bool PowerpointIndexExists, bool PdfIndexExists, int ItemsPerPage);
}