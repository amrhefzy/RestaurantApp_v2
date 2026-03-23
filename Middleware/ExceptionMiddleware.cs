using System.Net;
using System.Text.Json;
using FluentValidation;
using RestaurantMS.Helpers;
using Serilog;

namespace RestaurantMS.Middleware;

public class ExceptionMiddleware(RequestDelegate next, IWebHostEnvironment env)
{
    private readonly RequestDelegate _next = next;
    private readonly IWebHostEnvironment _env = env;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        ApiResponse<object> response;

        switch (ex)
        {
            case ValidationException validationEx:
            {
                context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                var errors = validationEx.Errors
                    .Select(e => e.ErrorMessage)
                    .Distinct()
                    .ToList();
                response = ApiResponse<object>.ValidationFail(errors);
                Log.Warning(validationEx, "Validation failure on {Path}", context.Request.Path);
                break;
            }

            case UnauthorizedAccessException:
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response = ApiResponse<object>.Fail("Unauthorized. Please login.", 401);
                Log.Warning(ex, "Unauthorized access attempt on {Path}", context.Request.Path);
                break;
            }

            case KeyNotFoundException:
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response = ApiResponse<object>.Fail(ex.Message, 404);
                Log.Warning(ex, "Resource not found on {Path}", context.Request.Path);
                break;
            }

            default:
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                var message = _env.IsDevelopment()
                    ? ex.ToString()
                    : "An unexpected error occurred. Please try again later.";
                response = ApiResponse<object>.Fail(message, 500);
                Log.Error(ex,
                    "Unhandled exception on {Method} {Path} — {Message}",
                    context.Request.Method,
                    context.Request.Path,
                    ex.Message);
                break;
            }
        }

        var json = JsonSerializer.Serialize(response, _jsonOptions);
        await context.Response.WriteAsync(json);
    }
}
