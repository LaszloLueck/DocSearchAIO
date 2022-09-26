using LanguageExt;

namespace DocSearchAIO.Endpoints.Suggest;

[Record]
public sealed record SuggestEntry(string Label, string[] IndexNames);