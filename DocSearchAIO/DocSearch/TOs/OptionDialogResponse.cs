using System.Collections.Generic;

namespace DocSearchAIO.DocSearch.TOs
{
    public class OptionDialogResponse
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

        public static implicit operator OptionDialogResponse(OptionDialogRequest request) => new()
        {
            ExcelIndexExists = request.ExcelIndexExists,
            FilterExcel = request.FilterExcel,
            FilterPdf = request.FilterPdf,
            FilterPowerpoint = request.FilterPowerpoint,
            FilterWord = request.FilterWord,
            ItemsPerPage = request.ItemsPerPage,
            PdfIndexExists = request.PdfIndexExists,
            PowerpointIndexExists = request.PowerpointIndexExists,
            WordIndexExists = request.WordIndexExists
        };
    }
}