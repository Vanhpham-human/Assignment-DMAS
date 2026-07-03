using Microsoft.EntityFrameworkCore;
using VnExpressCrawler.Data.Repositories;
using VnExpressCrawler.Shared.Models;

namespace VnExpressCrawler.Consumer.Services;

public interface IArticleProcessingService
{
    Task<bool> ProcessAndSaveAsync(Article article, CancellationToken cancellationToken = default);
}

public class ArticleProcessingService : IArticleProcessingService
{
    private readonly IArticleRepository _articleRepository;
    private readonly ILogger<ArticleProcessingService> _logger;

    public ArticleProcessingService(
        IArticleRepository articleRepository,
        ILogger<ArticleProcessingService> logger)
    {
        _articleRepository = articleRepository;
        _logger = logger;
    }

    public async Task<bool> ProcessAndSaveAsync(Article article, CancellationToken cancellationToken = default)
    {
        var exists = await _articleRepository.ExistsByUrlAsync(article.Url, cancellationToken);
        if (exists)
        {
            _logger.LogInformation("Bài viết đã tồn tại (idempotent), bỏ qua: {Url}", article.Url);
            return true;
        }

        try
        {
            await _articleRepository.AddAsync(article, cancellationToken);
            await _articleRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Đã lưu bài viết: [{Title}]", article.Title);
            return true;
        }
        catch (DbUpdateException ex) when (IsDuplicateUrlException(ex))
        {
            _logger.LogInformation("Bài viết đã được consumer khác lưu trước (race condition), bỏ qua: {Url}", article.Url);
            return true;
        }
    }

    private static bool IsDuplicateUrlException(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)
            || message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
            || message.Contains("1062", StringComparison.OrdinalIgnoreCase);
    }
}
