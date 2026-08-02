using System.Text.Json;
using Aganox.Lms.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Aganox.Lms.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private static async Task HandleAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;
        Log.Error(exception, "Unhandled exception. CorrelationId={CorrelationId}", correlationId);

        var (status, title, errors) = exception switch
        {
            ValidationException ve => (StatusCodes.Status400BadRequest, "Validation failed", ve.Errors),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found", null),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict", null),
            DomainException => (StatusCodes.Status400BadRequest, "Domain error", null),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null)
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : exception.Message,
            Instance = context.Request.Path
        };
        problem.Extensions["correlationId"] = correlationId;
        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = status;
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var correlationId) ||
            string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        context.TraceIdentifier = correlationId!;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId!;
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
