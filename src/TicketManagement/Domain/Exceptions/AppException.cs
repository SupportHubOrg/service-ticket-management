using System.Net;

namespace SupportHub.TicketManagement.Domain.Exceptions;

/// <summary>
/// Custom exception class for application-specific errors, allowing for an associated HTTP status code to be returned in API responses.
/// </summary>
public class AppException : Exception
{
    public int StatusCode { get; }

    //Constructor that accepts an integer status code and a message
    public AppException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
    
    //Constructor that accepts an HttpStatusCode enum value and a message
    public AppException(HttpStatusCode statusCode, string message) : base(message)
    {
        StatusCode = (int)statusCode;
    }
}