using LanguageExt;

namespace DocSearchAIO.Endpoints.Suggest;

[Record]
public record SuggestEntry(string Label, string[] IndexNames);