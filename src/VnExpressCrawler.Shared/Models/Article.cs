using System.ComponentModel.DataAnnotations;

namespace VnExpressCrawler.Shared.Models;

public class Article
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(500, ErrorMessage = "Tiêu đề tối đa 500 ký tự")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mô tả không được để trống")]
    [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nội dung không được để trống")]
    public string Content { get; set; } = string.Empty;

    [Required(ErrorMessage = "URL không được để trống")]
    [StringLength(2048, ErrorMessage = "URL tối đa 2048 ký tự")]
    [Url(ErrorMessage = "URL không hợp lệ")]
    public string Url { get; set; } = string.Empty;

    public DateTime CrawledAt { get; set; } = DateTime.UtcNow;
}
