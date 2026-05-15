using EngiFlow.Application.Exceptions;
using EngiFlow.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AppValidationException = EngiFlow.Application.Exceptions.ValidationException;

namespace EngiFlow.Api.ExceptionHandling;

/// <summary>
/// Converts application, domain, and unexpected exceptions into RFC 7807 problem details responses.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionHandler"/> class.
    /// </summary>
    /// <param name="logger">The structured logger used for server-side exception diagnostics.</param>
    /// <param name="problemDetailsService">The ASP.NET Core service that serializes problem details responses.</param>
    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var problemDetails = CreateProblemDetails(httpContext, exception);
        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        LogException(exception, httpContext.Response.StatusCode);

        await _problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        }).ConfigureAwait(false);

        return true;
    }

    private static ProblemDetails CreateProblemDetails(HttpContext httpContext, Exception exception)
    {
        return exception switch
        {
            AuthenticationFailedException authenticationFailedException => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication failed.",
                Detail = authenticationFailedException.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                Instance = httpContext.Request.Path
            },
            UnauthorizedAccessException unauthorizedAccessException => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized.",
                Detail = unauthorizedAccessException.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                Instance = httpContext.Request.Path
            },
            AppValidationException validationException => new ValidationProblemDetails(
                validationException.Errors.ToDictionary(
                    error => error.Key,
                    error => error.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed.",
                Detail = "One or more request values failed validation.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Instance = httpContext.Request.Path
            },
            EntityNotFoundException notFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found.",
                Detail = notFoundException.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Instance = httpContext.Request.Path
            },
            DomainException domainException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Business rule violation.",
                Detail = domainException.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                Instance = httpContext.Request.Path
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "An unexpected error occurred while processing the request.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                Instance = httpContext.Request.Path
            }
        };
    }

    private void LogException(Exception exception, int statusCode)
    {
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception produced a {StatusCode} response.", statusCode);
            return;
        }

        _logger.LogInformation(exception, "Handled exception produced a {StatusCode} response.", statusCode);
    }
}
