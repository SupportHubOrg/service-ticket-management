using System.Security.Claims;
using MediatR;

using SupportHub.TicketManagement.Domain.Enums;

namespace SupportHub.TicketManagement.Features.TicketMessages.GetTicketMessages;

public static class GetTicketMessagesEndpoint
{
    /// <summary>
    /// Maps the endpoint for retrieving a collection of tickets based on provided filters.
    /// </summary>
    public static void MapGetTicketMessagesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tickets/messages", async (
            [AsParameters] GetTicketMessagesRequest parameters,
            HttpContext httpContext,
            ClaimsPrincipal claimsPrincipal,
            IMediator mediator) =>
        {
            //Extract user ID from JWT claims
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
                return Results.Unauthorized();

            //Construct the query with the provided filters and pagination parameters
            var query = new GetTicketMessagesQuery(
                UserId: userId,
                TicketId: parameters.TicketId,
                SenderId: parameters.SenderId,
                SenderType: parameters.SenderType,
                SentAfter: parameters.SentAfter,
                SentBefore: parameters.SentBefore,
                SortBy: parameters.SortBy,
                SortDescending: parameters.SortDescending ?? true,
                PageNumber: parameters.PageNumber ?? 1,
                PageSize: parameters.PageSize ?? 20
            );

            //Send the query to retrieve the list of ticket messagess
            var result = await mediator.Send(query);

            //Return 200 OK with the list of ticket message summaries and pagination metadata
            return Results.Ok(result);
        }).RequireAuthorization();
    }
}

/// <summary>
/// HTTP request model bound from the query string.
/// </summary>
public record GetTicketMessagesRequest(
    Guid? TicketId,
    Guid? SenderId,
    Role? SenderType,
    DateTime? SentAfter,
    DateTime? SentBefore,
    string? SortBy,
    bool? SortDescending,
    int? PageNumber,
    int? PageSize
);