namespace VnExpressCrawler.Shared.Options;

public class CrawlerOptions
{
    public const string SectionName = "Crawler";

    public string CategoryUrl { get; set; } = "https://vnexpress.net/the-thao";
    public int CrawlIntervalSeconds { get; set; } = 300;
    public int MaxRetryCount { get; set; } = 3;
}
