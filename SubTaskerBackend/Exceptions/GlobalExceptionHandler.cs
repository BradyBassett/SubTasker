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

            // Log the exception details
            _logger.LogError(exception, "An unhandled exception occurred. TraceId: {TraceId}", traceId);

            // ProblemDetails response
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "Please try again later or contact support if the issue persists.",
                Instance = context.Request.Path
            };

            problemDetails.Extensions["traceId"] = traceId;

            await _problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                Exception = exception,
                ProblemDetails = problemDetails
            });

            // return true when handled, false otherwise
            return true;
        }
    }
}