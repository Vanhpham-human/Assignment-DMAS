using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using VnExpressCrawler.Shared.Options;

namespace VnExpressCrawler.Producer.Services;

public interface IUrlCrawlerService
{
    Task<IReadOnlyList<string>> CrawlArticleUrlsAsync(CancellationToken cancellationToken = default);
}

public class UrlCrawlerService : IUrlCrawlerService
{
    private readonly HttpClient _httpClient;
    private readonly IUrlFilterService _urlFilterService;
    private readonly CrawlerOptions _options;
    private readonly ILogger<UrlCrawlerService> _logger;

    public UrlCrawlerService(
        HttpClient httpClient,
        IUrlFilterService urlFilterService,
        IOptions<CrawlerOptions> options,
        ILogger<UrlCrawlerService> logger)
    {
        _httpClient = httpClient;
        _urlFilterService = urlFilterService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> CrawlArticleUrlsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Đang truy cập trang chuyên mục: {CategoryUrl}", _options.CategoryUrl);

        var html = await _httpClient.GetStringAsync(_options.CategoryUrl, cancellationToken);
        var document = new HtmlDocument();
        document.LoadHtml(html);

        var rawUrls = document.DocumentNode
            .SelectNodes("//a[@href]")
            ?.Select(node => node.GetAttributeValue("href", string.Empty))
            .Where(href => !string.IsNullOrWhiteSpace(href))
            .ToList() ?? [];

        _logger.LogInformation("Tìm thấy {Count} liên kết thô trên trang", rawUrls.Count);

        var filteredUrls = _urlFilterService
            .FilterValidArticleUrls(rawUrls)
            .ToList();

        _logger.LogInformation("Sau khi lọc còn {Count} URL bài viết hợp lệ", filteredUrls.Count);

        return filteredUrls;
    }
}
