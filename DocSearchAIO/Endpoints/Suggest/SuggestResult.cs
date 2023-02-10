using LanguageExt;

namespace DocSearchAIO.Endpoints.Suggest;

[Record]
public sealed record SuggestResult(string SearchPhrase, IEnumerable<SuggestEntry> Suggests);