using VnExpressCrawler.Producer.Services;

namespace VnExpressCrawler.Producer.Workers;

public class CrawlerWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CrawlerWorker> _logger;

    public CrawlerWorker(IServiceProvider serviceProvider, ILogger<CrawlerWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Producer Crawler Service đã khởi động");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var crawler = scope.ServiceProvider.GetRequiredService<IUrlCrawlerService>();
                var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();
                var options = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<VnExpressCrawler.Shared.Options.CrawlerOptions>>().Value;

                var urls = await crawler.CrawlArticleUrlsAsync(stoppingToken);

                foreach (var url in urls)
                {
                    publisher.PublishUrl(url);
                }

                _logger.LogInformation("Hoàn tất chu kỳ crawl. Đã gửi {Count} URL. Chờ {Interval}s...",
                    urls.Count, options.CrawlIntervalSeconds);

                await Task.Delay(TimeSpan.FromSeconds(options.CrawlIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong chu kỳ crawl. Thử lại sau 30 giây...");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("Producer Crawler Service đã dừng");
    }
}
