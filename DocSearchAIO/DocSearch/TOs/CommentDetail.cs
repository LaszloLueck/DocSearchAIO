using System;

namespace DocSearchAIO.DocSearch.TOs
{
    public record CommentDetail(string CommentText, string Author, DateTime Date, string Id, string Initials);
}