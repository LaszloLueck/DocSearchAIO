namespace DocSearchAIO.DocSearch.TOs
{
    public record OptionDialogResponse(bool FilterWord, bool FilterExcel, bool FilterPowerpoint, bool FilterPdf, bool FilterMsg, bool FilterEml,
        int ItemsPerPage)
    {
        public bool WordIndexExists { get; set; }
        public bool ExcelIndexExists { get; set; }
        public bool PowerpointIndexExists { get; set; }
        public bool PdfIndexExists { get; set; }

        public bool MsgIndexExists { get; set; }

        public bool EmlIndexExists { get; set; }

        public static implicit operator OptionDialogResponse(OptionDialogRequest request) => new(
            request.FilterWord, request.FilterExcel, request.FilterPowerpoint, request.FilterPdf, request.FilterMsg, request.FilterEml,
            request.ItemsPerPage
        );
    }
}