namespace DocSearchAIO.DocSearch.TOs
{
    public record DoSearchRequest(string SearchPhrase, int From, int Size, bool FilterWord, bool FilterExcel,
        bool FilterPowerpoint, bool FilterPdf);
}