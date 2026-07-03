namespace VnExpressCrawler.Shared.Constants;

public static class RabbitMqConstants
{
    public const string ArticleQueueName = "vnexpress.article.urls";
    public const string ArticleErrorQueueName = "vnexpress.article.urls.error";
    public const string ExchangeName = "vnexpress.crawler";
}
