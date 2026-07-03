using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using VnExpressCrawler.Shared.Constants;
using VnExpressCrawler.Shared.Options;

namespace VnExpressCrawler.Producer.Services;

public interface IRabbitMqPublisher
{
    void PublishUrl(string url);
}

public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;
        var config = options.Value;

        var factory = new ConnectionFactory
        {
            HostName = config.HostName,
            Port = config.Port,
            UserName = config.UserName,
            Password = config.Password,
            VirtualHost = config.VirtualHost,
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

        _logger.LogInformation("Đã kết nối RabbitMQ và khai báo hàng đợi bền vững: {QueueName}",
            RabbitMqConstants.ArticleQueueName);
    }

    public void PublishUrl(string url)
    {
        var body = Encoding.UTF8.GetBytes(url);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "text/plain";
        properties.DeliveryMode = 2;

        _channel.BasicPublish(
            exchange: string.Empty,
            routingKey: RabbitMqConstants.ArticleQueueName,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Đã gửi URL vào hàng đợi: {Url}", url);
    }

    public void Dispose()
    {
        _channel.Close();
        _connection.Close();
        _channel.Dispose();
        _connection.Dispose();
    }
}
