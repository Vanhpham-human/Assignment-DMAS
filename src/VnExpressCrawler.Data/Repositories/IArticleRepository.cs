using VnExpressCrawler.Shared.Models;

namespace VnExpressCrawler.Data.Repositories;

public interface IArticleRepository
{
    Task<bool> ExistsByUrlAsync(string url, CancellationToken cancellationToken = default);
    Task<Article?> GetByUrlAsync(string url, CancellationToken cancellationToken = default);
    Task<Article> AddAsync(Article article, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
