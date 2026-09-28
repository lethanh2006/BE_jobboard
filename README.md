# JobBoard Backend

Backend base for JobBoard, built with .NET 10 LTS and a layered monolith architecture.

## Structure

```text
src/
  JobBoard.Domain/          Entities, value objects, and business rules
  JobBoard.Application/     Use cases and application contracts
  JobBoard.Infrastructure/  Implementations for external/system concerns
  JobBoard.Api/             ASP.NET Core Controllers and composition root
tests/
  JobBoard.Domain.Tests/
  JobBoard.Application.Tests/
```

Dependency direction points inward:

```text
Api -> Infrastructure -> Application -> Domain
 |           |                            ^
 +-----------+----------------------------+
```

`Domain` không tham chiếu project nào khác. Các project ngoài chỉ phụ thuộc theo
chiều hướng vào trong.

## Requirements

- .NET SDK 10.0.100 or a newer .NET 10 patch (selected by `global.json`)
- Docker with Compose (optional, only for PostgreSQL mode)

## Run locally

From this `be` directory:

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet run --project src/JobBoard.Api --launch-profile http
```

The default `InMemory` provider needs no external service. To run the persistent
PostgreSQL mode:

```bash
docker compose up -d --wait
Persistence__Provider=PostgreSql \
Persistence__ApplyMigrations=true \
dotnet run --project src/JobBoard.Api --launch-profile http
```

The local container listens on port `5434` to avoid taking the usual PostgreSQL
port. `ApplyMigrations=true` applies committed migrations and seeds an empty
database. For a controlled deployment, run `dotnet tool restore`, execute
`dotnet ef database update`, and keep automatic migration disabled.

Development endpoints:

- API: `http://localhost:5000`
- OpenAPI document: `GET http://localhost:5000/openapi/v1.json`
- Optional HTTPS profile: `https://localhost:7000`

HTTPS redirection is disabled only in Development so the frontend can call the HTTP development URL without requiring a trusted local certificate.

## Configuration

ASP.NET Core loads `appsettings.json`, then the environment-specific file, then
environment variables. Development allows the frontend origin
`http://localhost:3000` through `Cors:AllowedOrigins`. JWT development settings
are merged from `appsettings.Development.json`. Production must provide a private
signing key of at least 32 bytes through `Jwt__SigningKey`; the application fails
fast when this secret is missing.

`Persistence:Provider` accepts `InMemory` (default) or `PostgreSql`. PostgreSQL
mode requires `ConnectionStrings:JobBoard`; override it with
`ConnectionStrings__JobBoard` outside local development.

Use double underscores to override nested settings:

```bash
ASPNETCORE_ENVIRONMENT=Development \
Cors__AllowedOrigins__0=http://localhost:3000 \
Jwt__SigningKey='replace-with-a-long-random-production-secret' \
dotnet run --project src/JobBoard.Api --launch-profile http
```

Do not reuse the development key. Use environment variables or .NET user secrets
for every deployed environment.

## Demo accounts

Both persistence providers seed these local-development users when empty:

| Role | Email | Password |
| --- | --- | --- |
| Candidate | `candidate@jobboard.vn` | `JobBoard@123` |
| Employer (company 1) | `employer@jobboard.vn` | `JobBoard@123` |
| Admin | `admin@jobboard.vn` | `JobBoard@123` |

Passwords are stored as salted PBKDF2 hashes. `POST /api/auth/login` returns a
15-minute bearer token and a seven-day rotating refresh token; only the refresh
token's SHA-256 hash is persisted. `GET /api/auth/me` returns the current
principal.

## Current scope

Milestone 1–7 đã thêm các entity domain cơ bản, value object `SalaryRange`,
`Skill` và trạng thái tin tuyển dụng. `User` là lớp trừu tượng dùng chung cho
`Candidate`, `Employer`, `AdminUser`; quyền quản lý tin được quyết định đa hình
qua `CanManage()`. Các constructor bảo vệ invariant; `Job` nhận `IClock` để kiểm
tra deadline mà không phụ thuộc trực tiếp vào thời gian hệ thống.
`ApplicationService` bảo vệ luồng ứng tuyển khỏi tin đóng, tin hết hạn và đơn
trùng; quyền đổi trạng thái đơn được kiểm tra qua chính mô hình domain. Service
nhận repository qua constructor và chỉ phụ thuộc các interface ở Application.
`JobSearchService` lọc và phân trang qua LINQ rồi trả DTO tóm tắt; các chiến
lược chấm điểm kỹ năng/lương có thể ghép qua `CompositeScorer`. Sự kiện
`Job.Published` cho phép thành phần khác lắng nghe mà không tạo dependency ngược.
Các repository và use case dùng API bất đồng bộ. Infrastructure hỗ trợ hai chế
độ: JSON/in-memory để khởi động nhanh, hoặc EF Core/PostgreSQL với migration,
index, foreign key, sequence cấp mã và unique constraint chống ứng tuyển trùng.

Web API hiện cung cấp:

- `GET /api/health` kiểm tra trạng thái dịch vụ.
- `GET /api/jobs` tìm kiếm, lọc, sắp xếp và phân trang tin đang tuyển.
- `GET /api/jobs/{id}` lấy đầy đủ nội dung một tin tuyển dụng.
- `POST /api/auth/login` xác thực tài khoản và cấp JWT; `GET /api/auth/me` đọc
  người dùng hiện tại.
- `POST /api/auth/register/candidate` tạo đồng thời tài khoản và hồ sơ ứng viên,
  rồi cấp phiên đăng nhập đầu tiên. Email được chuẩn hóa và bảo vệ bằng unique
  constraint trong PostgreSQL.
- `POST /api/auth/refresh` xoay refresh token một lần và cấp access token mới;
  tái sử dụng token cũ sẽ thu hồi toàn bộ token cùng phiên.
- `POST /api/auth/logout` thu hồi refresh token hiện tại ở phía server.
- `PUT /api/auth/password` xác minh mật khẩu hiện tại, cập nhật mật khẩu mới và
  thu hồi toàn bộ refresh token của tài khoản.
- `POST /api/applications` nộp đơn bằng danh tính ứng viên trong JWT và tự cấp
  mã đơn ở phía server.
- `GET /api/candidates/{id}/applications` xem lịch sử của chính ứng viên.
- `GET/PUT /api/candidates/{id}/profile` đọc và cập nhật hồ sơ, khoảng lương,
  kỹ năng của ứng viên.
- `PATCH /api/applications/{id}/status` cập nhật trạng thái đơn có kiểm tra công
  ty quản lý tin.
- Nhóm `/api/employers/{companyId}` hỗ trợ xem/tạo/đăng/đóng tin và xem ứng viên;
  company scope được đối chiếu với JWT.

Lỗi validation/nghiệp vụ được chuẩn hóa về `ProblemDetails`. Dữ liệu Job trả về
đã có cùng contract mà frontend cần: công ty, địa điểm, cấp bậc, hình thức làm
việc, nội dung chi tiết, kỹ năng và thời hạn.

Xác thực hiện dùng JWT, PBKDF2 và refresh token rotation có phát hiện reuse;
người dùng có thể đổi mật khẩu và buộc các phiên dùng refresh token đăng nhập
lại. Ứng viên có thể tự đăng ký và được tạo hồ sơ trong cùng transaction. Account,
phiên, hồ sơ, tin và đơn có thể được lưu bền vững bằng PostgreSQL. Chưa có xác
minh email, khôi phục mật khẩu, MediatR, AutoMapper, cache, queue hay background
job; các phần này sẽ được thêm theo đúng milestone trong tài liệu kế hoạch.
