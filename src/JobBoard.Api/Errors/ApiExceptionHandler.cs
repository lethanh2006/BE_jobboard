using JobBoard.Application.Exceptions;
using JobBoard.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.Api.Errors;

public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Đăng nhập thất bại"),
            InvalidRefreshTokenException => (StatusCodes.Status401Unauthorized, "Phiên đăng nhập không hợp lệ"),
            ResourceNotFoundException => (StatusCodes.Status404NotFound, "Không tìm thấy tài nguyên"),
            JobManagementForbiddenException => (StatusCodes.Status403Forbidden, "Không có quyền thực hiện"),
            AlreadyAppliedException => (StatusCodes.Status409Conflict, "Đơn ứng tuyển đã tồn tại"),
            DomainException => (StatusCodes.Status400BadRequest, "Không thể thực hiện nghiệp vụ"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Dữ liệu không hợp lệ"),
            _ => (StatusCodes.Status500InternalServerError, "Đã xảy ra lỗi hệ thống")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Lỗi không được xử lý khi gọi {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = statusCode == StatusCodes.Status500InternalServerError
                    ? "Vui lòng thử lại sau."
                    : exception.Message,
                Instance = httpContext.Request.Path
            },
            Exception = exception
        });
    }
}
