using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

using SupportHub.TicketManagement.Domain.Exceptions;

namespace SupportHub.TicketManagement.Infrastructure.Middleware;

/// <summary>
/// Global exception handler middleware that catches all unhandled exceptions thrown during request processing and converts them into standardized HTTP responses.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        //Create a ProblemDetails object to structure the error response
        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        //Check if the exception is an AppException
        if (exception is AppException appEx)
        {
            //Log the application error with a warning level
            _logger.LogWarning("Application error occurred: {Message}", appEx.Message);

            problemDetails.Status = appEx.StatusCode;
            problemDetails.Title = "Application Error";
            problemDetails.Detail = appEx.Message;
        }
        else
        {
            //Log the unexpected system error with an error level
            _logger.LogError(exception, "An unexpected system exception occurred.");

            problemDetails.Status = StatusCodes.Status500InternalServerError;
            problemDetails.Title = "Server Error";
            problemDetails.Detail = "An unexpected error occurred while processing the request.";
        }

        //Set the response status code and write the problem details as JSON
        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true; 
    }
}