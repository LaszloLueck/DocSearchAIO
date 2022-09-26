using LanguageExt;

namespace DocSearchAIO.DocSearch.TOs;

[Record]
public sealed record ElementsToHash(string Category, DateTime Created,
    string ContentString, string Creator, string Description, string Identifier,
    string Keywords, string Language, DateTime Modified, string Revision,
    string Subject, string Title, string Version, string ContentStatus,
    string ContentType, DateTime LastPrinted, string LastModifiedBy);