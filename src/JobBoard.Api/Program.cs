using System.Text.Json.Serialization;
using JobBoard.Api.Errors;
using JobBoard.Application.Abstractions.Persistence;
using JobBoard.Application.Jobs;
using JobBoard.Domain.Abstractions;
using JobBoard.Infrastructure.Persistence;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IJobRepository>(serviceProvider =>
{
    var seedFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "jobs.json");
    return new JsonJobRepository(seedFilePath, serviceProvider.GetRequiredService<IClock>());
});
builder.Services.AddScoped<JobSearchService>();
builder.Services.AddScoped<JobDetailService>();
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
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));
app.MapControllers();

app.Run();

public partial class Program;
