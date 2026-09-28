using System.Text;
using System.Text.Json.Serialization;
using JobBoard.Api.Authentication;
using JobBoard.Api.Errors;
using JobBoard.Application.Abstractions.Authentication;
using JobBoard.Application.Abstractions.Persistence;
using JobBoard.Application.Applications;
using JobBoard.Application.Authentication;
using JobBoard.Application.Candidates;
using JobBoard.Application.Jobs;
using JobBoard.Domain.Abstractions;
using JobBoard.Infrastructure.Authentication;
using JobBoard.Infrastructure.Persistence;
using JobBoard.Infrastructure.Persistence.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];
var jwtOptions = builder.Configuration
    .GetRequiredSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? throw new InvalidOperationException("Thiếu cấu hình JWT.");
var persistenceProvider = builder.Configuration["Persistence:Provider"] ?? "InMemory";
var usePostgres = persistenceProvider switch
{
    "InMemory" => false,
    "PostgreSql" => true,
    _ => throw new InvalidOperationException(
        "Persistence:Provider chỉ nhận giá trị InMemory hoặc PostgreSql.")
};

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
    string.IsNullOrWhiteSpace(jwtOptions.Audience) ||
    jwtOptions.AccessTokenMinutes <= 0 ||
    Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
{
    throw new InvalidOperationException(
        "Cấu hình JWT không hợp lệ; signing key phải dài ít nhất 32 byte.");
}

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<IAccessTokenGenerator, JwtAccessTokenGenerator>();

if (usePostgres)
{
    var connectionString = builder.Configuration.GetConnectionString("JobBoard");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:JobBoard là bắt buộc khi dùng PostgreSql.");
    }

    builder.Services.AddDbContext<JobBoardDbContext>(options =>
        options.UseNpgsql(connectionString));
    builder.Services.AddScoped<IUserAccountRepository, PostgresUserAccountRepository>();
    builder.Services.AddScoped<IJobRepository, PostgresJobRepository>();
    builder.Services.AddScoped<IJobApplicationRepository, PostgresJobApplicationRepository>();
    builder.Services.AddScoped<ICandidateRepository, PostgresCandidateRepository>();
    builder.Services.AddScoped<JobBoardDatabaseInitializer>();
}
else
{
    builder.Services.AddSingleton<IUserAccountRepository, InMemoryUserAccountRepository>();
    builder.Services.AddSingleton<IJobRepository>(serviceProvider =>
    {
        var seedFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "jobs.json");
        return new JsonJobRepository(seedFilePath, serviceProvider.GetRequiredService<IClock>());
    });
    builder.Services.AddSingleton<IJobApplicationRepository>(
        new InMemoryJobApplicationRepository(seedDemoData: true));
    builder.Services.AddSingleton<ICandidateRepository>(
        new InMemoryCandidateRepository(seedDemoData: true));
}

builder.Services.AddScoped<JobSearchService>();
builder.Services.AddScoped<JobDetailService>();
builder.Services.AddScoped<JobManagementService>();
builder.Services.AddScoped<ApplicationService>();
builder.Services.AddScoped<ApplicationQueryService>();
builder.Services.AddScoped<CandidateProfileService>();
builder.Services.AddScoped<LoginService>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = JobBoardClaimTypes.Name,
            RoleClaimType = JobBoardClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Chưa xác thực",
                    Detail = "Vui lòng đăng nhập để tiếp tục.",
                    Instance = context.Request.Path
                });
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Không có quyền thực hiện",
                    Detail = "Tài khoản không có quyền truy cập tài nguyên này.",
                    Instance = context.Request.Path
                });
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

if (usePostgres && builder.Configuration.GetValue<bool>("Persistence:ApplyMigrations"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var initializer = scope.ServiceProvider.GetRequiredService<JobBoardDatabaseInitializer>();
    var seedFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "jobs.json");
    await initializer.InitializeAsync(seedFilePath);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));
app.MapControllers();

app.Run();

public partial class Program;
