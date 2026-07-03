using System.Text.RegularExpressions;

namespace VnExpressCrawler.Producer.Services;

public interface IUrlFilterService
{
    IEnumerable<string> FilterValidArticleUrls(IEnumerable<string> urls);
}

public partial class UrlFilterService : IUrlFilterService
{
    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "vnexpress.net",
        "www.vnexpress.net"
    };

    [GeneratedRegex(@"^https?://(?:www\.)?vnexpress\.net/[\w\-]+-\d+\.html$", RegexOptions.IgnoreCase)]
    private static partial Regex ArticleUrlPattern();

    public IEnumerable<string> FilterValidArticleUrls(IEnumerable<string> urls)
    {
        return urls
            .Select(NormalizeUrl)
            .Where(IsValidVnExpressArticleUrl)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        var trimmed = url.Trim();

        if (trimmed.StartsWith("//", StringComparison.Ordinal))
            trimmed = "https:" + trimmed;

        return trimmed.Split('#')[0].Split('?')[0];
    }

    private static bool IsValidVnExpressArticleUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (!AllowedHosts.Contains(uri.Host))
            return false;

        if (uri.AbsolutePath.Contains("/tag/", StringComparison.OrdinalIgnoreCase))
            return false;

        if (uri.AbsolutePath.Contains("/video/", StringComparison.OrdinalIgnoreCase))
            return false;

        return ArticleUrlPattern().IsMatch(url);
    }
}
