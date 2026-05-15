using EngiFlow.Api.ExceptionHandling;
using EngiFlow.Application.Exceptions;
using EngiFlow.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using AppValidationException = EngiFlow.Application.Exceptions.ValidationException;

namespace EngiFlow.Api.Tests;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_WhenValidationException_WritesValidationProblemDetails()
    {
        var problemDetailsService = new CapturingProblemDetailsService();
        var handler = CreateHandler(problemDetailsService);
        var httpContext = CreateHttpContext();
        var exception = new AppValidationException(new Dictionary<string, string[]>
        {
            [nameof(Models.CreateEcoRequest.Title)] = ["Title is required."]
        });

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        var details = Assert.IsType<ValidationProblemDetails>(problemDetailsService.Context?.ProblemDetails);
        Assert.Equal(StatusCodes.Status400BadRequest, details.Status);
        Assert.Equal("Validation failed.", details.Title);
        Assert.Equal(["Title is required."], details.Errors[nameof(Models.CreateEcoRequest.Title)]);
    }

    [Fact]
    public async Task TryHandleAsync_WhenEntityNotFoundException_WritesNotFoundProblemDetails()
    {
        var problemDetailsService = new CapturingProblemDetailsService();
        var handler = CreateHandler(problemDetailsService);
        var httpContext = CreateHttpContext();
        var ecoId = Guid.NewGuid();

        var handled = await handler.TryHandleAsync(
            httpContext,
            new EntityNotFoundException("EngineeringChangeOrder", ecoId),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
        var details = Assert.IsType<ProblemDetails>(problemDetailsService.Context?.ProblemDetails);
        Assert.Equal(StatusCodes.Status404NotFound, details.Status);
        Assert.Equal("Resource not found.", details.Title);
        Assert.Contains(ecoId.ToString(), details.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TryHandleAsync_WhenDomainException_WritesConflictProblemDetails()
    {
        var problemDetailsService = new CapturingProblemDetailsService();
        var handler = CreateHandler(problemDetailsService);
        var httpContext = CreateHttpContext();

        var handled = await handler.TryHandleAsync(
            httpContext,
            new DomainException("ECO cannot transition from Draft to Approved."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        var details = Assert.IsType<ProblemDetails>(problemDetailsService.Context?.ProblemDetails);
        Assert.Equal(StatusCodes.Status409Conflict, details.Status);
        Assert.Equal("Business rule violation.", details.Title);
        Assert.Equal("ECO cannot transition from Draft to Approved.", details.Detail);
    }

    [Fact]
    public async Task TryHandleAsync_WhenUnhandledException_WritesGenericServerProblemDetails()
    {
        var problemDetailsService = new CapturingProblemDetailsService();
        var handler = CreateHandler(problemDetailsService);
        var httpContext = CreateHttpContext();

        var handled = await handler.TryHandleAsync(
            httpContext,
            new InvalidOperationException("Do not leak this detail."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        var details = Assert.IsType<ProblemDetails>(problemDetailsService.Context?.ProblemDetails);
        Assert.Equal(StatusCodes.Status500InternalServerError, details.Status);
        Assert.Equal("An unexpected error occurred.", details.Title);
        Assert.DoesNotContain("Do not leak this detail.", details.Detail, StringComparison.Ordinal);
    }

    private static GlobalExceptionHandler CreateHandler(IProblemDetailsService problemDetailsService)
    {
        return new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance,
            problemDetailsService);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/ecos";
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    private sealed class CapturingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetailsContext? Context { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Context = context;
            return ValueTask.CompletedTask;
        }
    }
}
