namespace DocSearchAIO.DocSearch.TOs
{
    public class OptionDialogRequest
    {
        public bool FilterWord { get; set; }
        public bool FilterExcel { get; set; }
        public bool FilterPowerpoint { get; set; }
        public bool FilterPdf { get; set; }
        public bool WordIndexExists { get; set; }
        public bool ExcelIndexExists { get; set; }
        public bool PowerpointIndexExists { get; set; }
        public bool PdfIndexExists { get; set; }
        public int ItemsPerPage { get; set; }
    }
}