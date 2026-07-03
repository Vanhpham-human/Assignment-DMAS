using HtmlAgilityPack;
using VnExpressCrawler.Shared.Models;

namespace VnExpressCrawler.Consumer.Services;

public interface IArticleParserService
{
    Task<Article?> ParseArticleAsync(string url, CancellationToken cancellationToken = default);
}

public class ArticleParserService : IArticleParserService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ArticleParserService> _logger;

    public ArticleParserService(HttpClient httpClient, ILogger<ArticleParserService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Article?> ParseArticleAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Đang xử lý URL: {Url}", url);

        using var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Không thể truy cập URL {Url}. Status: {StatusCode}", url, response.StatusCode);
            return null;
        }

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var document = new HtmlDocument();
        document.LoadHtml(html);

        var titleNode = document.DocumentNode.SelectSingleNode("//h1[contains(@class,'title-detail')]")
            ?? document.DocumentNode.SelectSingleNode("//h1");

        var descriptionNode = document.DocumentNode.SelectSingleNode("//p[contains(@class,'description')]")
            ?? document.DocumentNode.SelectSingleNode("//meta[@name='description']");

        var contentNode = document.DocumentNode.SelectSingleNode("//article[contains(@class,'fck_detail')]")
            ?? document.DocumentNode.SelectSingleNode("//div[contains(@class,'fck_detail')]")
            ?? document.DocumentNode.SelectSingleNode("//article[contains(@class,'article-content')]");

        var title = CleanText(titleNode?.InnerText);
        var description = descriptionNode?.Name == "meta"
            ? descriptionNode.GetAttributeValue("content", string.Empty)
            : CleanText(descriptionNode?.InnerText);
        var content = CleanContent(contentNode?.InnerText);

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("Bóc tách thất bại cho URL {Url}: thiếu tiêu đề hoặc nội dung", url);
            return null;
        }

        if (string.IsNullOrWhiteSpace(description))
            description = title.Length > 200 ? title[..200] : title;

        return new Article
        {
            Title = title,
            Description = description,
            Content = content,
            Url = url,
            CrawledAt = DateTime.UtcNow
        };
    }

    private static string CleanText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return HtmlEntity.DeEntitize(text)
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();
    }

    private static string CleanContent(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return HtmlEntity.DeEntitize(text).Trim();
    }
}
