# Hệ thống Thu thập và Xử lý Dữ liệu Thông tin Bất đồng bộ - VnExpress Crawler

Hệ thống **Producer-Consumer** thu thập bài viết từ chuyên mục **Thể thao** của [VnExpress](https://vnexpress.net/the-thao), gửi URL qua **RabbitMQ**, và lưu nội dung chi tiết vào **MySQL**.

## Video demo

[Xem video ghi màn hình chạy dự án](https://drive.google.com/file/d/184ysEm6_GgbcRx5Jk_7uPDvo1ZqqPYrO/view?usp=sharing)

## Kiến trúc

```
┌─────────────────┐     RabbitMQ Queue      ┌──────────────────┐
│  Producer       │  ──────────────────────►│  Consumer        │
│  (Crawler)      │   vnexpress.article.urls│  (Processor)     │
└─────────────────┘                         └────────┬─────────┘
                                                     │
                                                     ▼
                                              ┌──────────────┐
                                              │    MySQL     │
                                              └──────────────┘
```

## Tech Stack

| Thành phần | Công nghệ |
|------------|-----------|
| Ngôn ngữ | C# / .NET 8.0 |
| Bóc tách HTML | HtmlAgilityPack |
| Message Queue | RabbitMQ |
| Database | MySQL + Pomelo EF Core |

## Cấu trúc Solution

```
src/
├── VnExpressCrawler.Shared/    # Models, Constants, Options
├── VnExpressCrawler.Data/      # DbContext, EF Configurations, Repository
├── VnExpressCrawler.Producer/  # Crawler Worker (Producer)
└── VnExpressCrawler.Consumer/  # Processor Worker (Consumer)
```

## Yêu cầu

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- MySQL và RabbitMQ (Docker **hoặc** cài qua Homebrew)

## Cài đặt và Chạy

### Cách 1: Dùng Docker

```bash
docker compose up -d
```

- MySQL: `localhost:3306` (user: `root`, password: `root123`, database: `vnexpress_crawler`)
- RabbitMQ: `localhost:5672` (Management UI: http://localhost:15672 — guest/guest)

### Cách 2: Dùng Homebrew (macOS)

```bash
brew services start mysql
brew services start rabbitmq
mysql -uroot -e "CREATE DATABASE IF NOT EXISTS vnexpress_crawler CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"
```

> Nếu MySQL không có mật khẩu, cấu hình `DefaultConnection` trong `src/VnExpressCrawler.Consumer/appsettings.json` với `Password=;`

### Chạy Consumer (Terminal 1)

```bash
dotnet run --project src/VnExpressCrawler.Consumer
```

Consumer tự chạy migration khi khởi động.

### Chạy Producer (Terminal 2)

```bash
dotnet run --project src/VnExpressCrawler.Producer
```

### Chạy nhiều Consumer (Đa nhiệm)

Mở thêm terminal và chạy lại lệnh Consumer. Hệ thống đảm bảo **idempotency** qua:
- Kiểm tra URL tồn tại trước khi insert
- Unique Index trên cột `Url`
- Xử lý race condition (DbUpdateException duplicate)

## Tính năng đã triển khai

### A. Tầng Dữ liệu (1.5đ)
- Model `Article`: Id, Title, Description, Content (longtext), Url (Unique), CrawledAt
- Data Annotations: `[Required]`, `[StringLength]`, `[Url]`
- EF Core + Pomelo MySQL + Migrations
- Unique Index trên `Url` tại Database

### B. Producer - Crawler (2.5đ)
- HtmlAgilityPack bóc tách `<a href>` từ trang chuyên mục Thể thao
- Lọc URL VnExpress hợp lệ (regex + whitelist domain)
- RabbitMQ durable queue, gửi URL dạng `byte[]`

### C. Consumer - Processor (3.0đ)
- `BasicQos(prefetchCount: 1)` điều tiết lưu lượng
- Bóc tách Title, Description, Content từ trang chi tiết
- `IServiceScopeFactory` tạo scoped DbContext trong BackgroundService

### D. Xử lý nâng cao (2.0đ)
- try-catch, retry tối đa 3 lần, Error Queue khi thất bại
- Idempotency cho nhiều Consumer chạy song song

### E. Chất lượng mã (1.0đ)
- Tách layer: Models / Data / Services / Workers
- Dependency Injection đầy đủ
- Logging tiếng Việt rõ ràng trên Terminal

## Cấu hình

**Producer** — `Crawler:CategoryUrl`, `Crawler:CrawlIntervalSeconds`, `RabbitMQ:*`

**Consumer** — `ConnectionStrings:DefaultConnection`, `Crawler:MaxRetryCount`, `RabbitMQ:*`

## Kiểm tra dữ liệu

```sql
USE vnexpress_crawler;
SELECT Id, Title, Url, CrawledAt FROM Articles ORDER BY CrawledAt DESC LIMIT 10;
```

## Ghi chú

- Producer crawl định kỳ mỗi 5 phút (chỉnh `CrawlIntervalSeconds` để demo nhanh hơn)
- Error Queue: `vnexpress.article.urls.error`
- User-Agent được thiết lập để tránh bị chặn cơ bản
