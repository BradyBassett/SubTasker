using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SubTaskerBackend.Exceptions
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IProblemDetailsService _problemDetailsService;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetailsService)
        {
            _logger = logger;
            _problemDetailsService = problemDetailsService;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
        {
            var traceId = context.TraceIdentifier;

            // If client disconnects, skip response
            if (cancellationToken.IsCancellationRequested || context.RequestAborted.IsCancellationRequested)
            {
                _logger.LogWarning("Request was cancelled by the client. TraceId: {TraceId}", traceId);

                return true; // Considered handled since client is gone
            }

            if (exception is HttpException authException)
            {
                _logger.LogWarning(exception, "Authentication error occurred. TraceId: {TraceId}", traceId);

                return await WriteErrorResponseAsync(
                    context,
                    authException.StatusCode,
                    "Authentication Error",
                    authException.Message,
                    traceId
                );
            }

            // Log the exception details
            _logger.LogError(exception, "An unhandled exception occurred. TraceId: {TraceId}", traceId);

            return await WriteErrorResponseAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                "Please try again later or contact support if the issue persists.",
                traceId
            );
        }

        private async Task<bool> WriteErrorResponseAsync(HttpContext context, int statusCode, string title, string detail, string traceId)
        {
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };

            problemDetails.Extensions["traceId"] = traceId;

            await _problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = problemDetails
            });

            return true;
        }
    }
}