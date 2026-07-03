using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using VnExpressCrawler.Consumer.Services;
using VnExpressCrawler.Shared.Constants;
using VnExpressCrawler.Shared.Options;

namespace VnExpressCrawler.Consumer.Workers;

public class ArticleProcessorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _rabbitMqOptions;
    private readonly CrawlerOptions _crawlerOptions;
    private readonly ILogger<ArticleProcessorWorker> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public ArticleProcessorWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IOptions<CrawlerOptions> crawlerOptions,
        ILogger<ArticleProcessorWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _rabbitMqOptions = rabbitMqOptions.Value;
        _crawlerOptions = crawlerOptions.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Consumer Processor Service đã khởi động");

        var factory = new ConnectionFactory
        {
            HostName = _rabbitMqOptions.HostName,
            Port = _rabbitMqOptions.Port,
            UserName = _rabbitMqOptions.UserName,
            Password = _rabbitMqOptions.Password,
            VirtualHost = _rabbitMqOptions.VirtualHost,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(
            queue: RabbitMqConstants.ArticleQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _channel.QueueDeclare(
            queue: RabbitMqConstants.ArticleErrorQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        _logger.LogInformation("Đã cấu hình BasicQos prefetchCount=1, lắng nghe hàng đợi: {QueueName}",
            RabbitMqConstants.ArticleQueueName);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, args) =>
        {
            await HandleMessageAsync(args, stoppingToken);
        };

        _channel.BasicConsume(
            queue: RabbitMqConstants.ArticleQueueName,
            autoAck: false,
            consumer: consumer);

        stoppingToken.Register(() =>
        {
            _logger.LogInformation("Consumer Processor Service đang dừng...");
        });

        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs args, CancellationToken stoppingToken)
    {
        var url = Encoding.UTF8.GetString(args.Body.ToArray());
        var retryCount = GetRetryCount(args.BasicProperties);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var parser = scope.ServiceProvider.GetRequiredService<IArticleParserService>();
            var processor = scope.ServiceProvider.GetRequiredService<IArticleProcessingService>();

            var article = await parser.ParseArticleAsync(url, stoppingToken);

            if (article is null)
            {
                await HandleFailureAsync(args, url, retryCount, "Bóc tách thất bại hoặc trang không tồn tại");
                return;
            }

            var saved = await processor.ProcessAndSaveAsync(article, stoppingToken);

            if (saved)
            {
                _channel!.BasicAck(args.DeliveryTag, multiple: false);
            }
            else
            {
                await HandleFailureAsync(args, url, retryCount, "Không thể lưu bài viết");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xử lý URL: {Url}", url);
            await HandleFailureAsync(args, url, retryCount, ex.Message);
        }
    }

    private async Task HandleFailureAsync(
        BasicDeliverEventArgs args,
        string url,
        int retryCount,
        string reason)
    {
        if (retryCount < _crawlerOptions.MaxRetryCount)
        {
            _logger.LogWarning("Retry URL ({Retry}/{Max}): {Url}. Lý do: {Reason}",
                retryCount + 1, _crawlerOptions.MaxRetryCount, url, reason);

            RequeueWithRetry(args, url, retryCount + 1);
            _channel!.BasicAck(args.DeliveryTag, multiple: false);
        }
        else
        {
            _logger.LogError("Đẩy URL vào Error Queue sau {Max} lần thử: {Url}", _crawlerOptions.MaxRetryCount, url);
            PublishToErrorQueue(url, reason);
            _channel!.BasicAck(args.DeliveryTag, multiple: false);
        }

        await Task.CompletedTask;
    }

    private void RequeueWithRetry(BasicDeliverEventArgs args, string url, int retryCount)
    {
        var properties = _channel!.CreateBasicProperties();
        properties.Persistent = true;
        properties.DeliveryMode = 2;
        properties.Headers = new Dictionary<string, object>
        {
            ["x-retry-count"] = retryCount
        };

        _channel.BasicPublish(
            exchange: string.Empty,
            routingKey: RabbitMqConstants.ArticleQueueName,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(url));
    }

    private void PublishToErrorQueue(string url, string reason)
    {
        var properties = _channel!.CreateBasicProperties();
        properties.Persistent = true;
        properties.DeliveryMode = 2;
        properties.Headers = new Dictionary<string, object>
        {
            ["x-error-reason"] = reason,
            ["x-failed-at"] = DateTime.UtcNow.ToString("O")
        };

        _channel.BasicPublish(
            exchange: string.Empty,
            routingKey: RabbitMqConstants.ArticleErrorQueueName,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(url));
    }

    private static int GetRetryCount(IBasicProperties? properties)
    {
        if (properties?.Headers is null)
            return 0;

        if (properties.Headers.TryGetValue("x-retry-count", out var value))
        {
            return value switch
            {
                int i => i,
                byte b => b,
                long l => (int)l,
                _ => 0
            };
        }

        return 0;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
