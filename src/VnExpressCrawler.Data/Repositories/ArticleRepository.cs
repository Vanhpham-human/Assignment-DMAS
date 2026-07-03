using Microsoft.EntityFrameworkCore;
using VnExpressCrawler.Shared.Models;

namespace VnExpressCrawler.Data.Repositories;

public class ArticleRepository : IArticleRepository
{
    private readonly ApplicationDbContext _context;

    public ArticleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        return _context.Articles.AnyAsync(a => a.Url == url, cancellationToken);
    }

    public Task<Article?> GetByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        return _context.Articles.FirstOrDefaultAsync(a => a.Url == url, cancellationToken);
    }

    public async Task<Article> AddAsync(Article article, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Articles.AddAsync(article, cancellationToken);
        return entry.Entity;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
