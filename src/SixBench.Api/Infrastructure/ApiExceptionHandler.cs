using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SixBench.Services.Exceptions;

namespace SixBench.Api.Infrastructure;

/// <summary>
/// Maps service exceptions to RFC 7807 problem responses.
/// </summary>
/// <param name="problemDetails">Problem details writer.</param>
/// <param name="logger">Logger.</param>
public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            ServiceValidationException => (StatusCodes.Status400BadRequest, "Invalid request"),
            FieldValidationException => (StatusCodes.Status400BadRequest, "Invalid request"),
            RokuUnreachableException => (StatusCodes.Status502BadGateway, "Roku unreachable"),
            RokuRequestRejectedException => (StatusCodes.Status409Conflict, "Roku refused the request"),
            CertificateRequestException => (StatusCodes.Status502BadGateway, "Certificate request failed"),
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested => (499, "Client closed request"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error"),
        };

        if (status >= 500 && status != StatusCodes.Status502BadGateway)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning("{Title}: {Message}", title, exception.Message);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = exception is FieldValidationException fields
                ? new ValidationProblemDetails(fields.Errors.ToDictionary(e => e.Key, e => e.Value)) { Status = status, Title = title }
                : new ProblemDetails
                {
                    Status = status,
                    Title = title,
                    Detail = status == StatusCodes.Status500InternalServerError ? "An unexpected error occurred." : exception.Message,
                },
        });
    }
}
