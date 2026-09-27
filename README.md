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

## Run locally

From this `be` directory:

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet run --project src/JobBoard.Api --launch-profile http
```

Development endpoints:

- API: `http://localhost:5000`
- OpenAPI document: `GET http://localhost:5000/openapi/v1.json`
- Optional HTTPS profile: `https://localhost:7000`

HTTPS redirection is disabled only in Development so the frontend can call the HTTP development URL without requiring a trusted local certificate.

## Configuration

ASP.NET Core loads `appsettings.json`, then the environment-specific file, then environment variables. Development allows the frontend origin `http://localhost:3000` through `Cors:AllowedOrigins`.

Use double underscores to override nested settings:

```bash
ASPNETCORE_ENVIRONMENT=Development \
Cors__AllowedOrigins__0=http://localhost:3000 \
dotnet run --project src/JobBoard.Api --launch-profile http
```

Do not commit secrets to appsettings files. Use environment variables or .NET user secrets when secrets are introduced.

## Current scope

Milestone 1–6 đã thêm các entity domain cơ bản, value object `SalaryRange`,
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

Chưa có repository hạ tầng, EF Core/PostgreSQL, auth, MediatR, AutoMapper,
cache, queue hay background job; các phần này sẽ được thêm theo đúng milestone
trong tài liệu kế hoạch.
