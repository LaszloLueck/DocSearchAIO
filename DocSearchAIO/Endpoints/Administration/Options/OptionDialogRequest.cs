using LanguageExt;

namespace DocSearchAIO.Endpoints.Administration.Options;

[Record]
public record OptionDialogRequest(bool FilterWord, bool FilterExcel, bool FilterPowerpoint, bool FilterPdf, bool FilterMsg, bool FilterEml,
    bool WordIndexExists, bool ExcelIndexExists, bool PowerpointIndexExists, bool PdfIndexExists, bool MsgIndexExists, bool EmlIndexExists,
    int ItemsPerPage);