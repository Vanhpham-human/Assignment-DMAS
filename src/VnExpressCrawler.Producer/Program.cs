using VnExpressCrawler.Producer.Services;
using VnExpressCrawler.Producer.Workers;
using VnExpressCrawler.Shared.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<CrawlerOptions>(
    builder.Configuration.GetSection(CrawlerOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));

builder.Services.AddHttpClient<IUrlCrawlerService, UrlCrawlerService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (compatible; VnExpressCrawler/1.0; +https://github.com/assignment)");
});

builder.Services.AddSingleton<IUrlFilterService, UrlFilterService>();
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddHostedService<CrawlerWorker>();

var host = builder.Build();
host.Run();
