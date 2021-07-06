namespace DocSearchAIO.DocSearch.TOs
{
    public class DoSearchRequest
    {
        public string SearchPhrase { get; set; } = string.Empty;
        public int From { get; set; }
        public int Size { get; set; } = 10;
        public bool FilterWord { get; set; }
        public bool FilterExcel { get; set; }
        public bool FilterPowerpoint { get; set; }
        public bool FilterPdf { get; set; }
    }
}