using DocSearchAIO.DocSearch.TOs;

namespace DocSearchAIO.Endpoints.Suggest;

public record SuggestResult(string SearchPhrase, IEnumerable<SuggestEntry> Suggests);