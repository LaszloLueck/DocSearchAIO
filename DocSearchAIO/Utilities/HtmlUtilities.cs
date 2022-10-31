using System.Text;
using HtmlAgilityPack;
using LanguageExt;

namespace DocSearchAIO.Utilities;

public static class HtmlUtilities
{
    public static async Task<Option<string>> ExtractTextFromHtml(this string htmlString, ILogger logger)
    {
        return await Task.Run(() =>
        {
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(htmlString);
                var sb = new StringBuilder();

                foreach (var node in doc.DocumentNode.DescendantsAndSelf())
                {
                    if (node.HasChildNodes) continue;
                    var text = node.InnerText ?? "";
                    if (!string.IsNullOrEmpty(text) && !text.StartsWith("<!--"))
                        sb.AppendLine(text);
                }
                return sb.ToString();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "error while converting text from html");
                return Option<string>.None;
            }
        });
    }
}