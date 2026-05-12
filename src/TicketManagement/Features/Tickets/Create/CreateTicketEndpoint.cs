using System.Security.Claims;
using MediatR;

namespace SupportHub.TicketManagement.Features.Tickets.CreateTicket;

public static class CreateTicketEndpoint
{
    /// <summary>
    /// Maps the endpoint for creating a new support ticket.
    /// </summary>
    /// <param name="app"></param>
    public static void MapCreateTicketEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tickets", async (
            CreateTicketRequest request,
            HttpContext httpContext,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            //Extract user ID from JWT claims
            var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
                return Results.Unauthorized();

            //Create and send the command
            var command = new CreateTicketCommand(userId, request.Subject, request.Description);
            var ticketId = await mediator.Send(command);

            //Return 201 Created with the new resource route
            return Results.Created($"/api/tickets/{ticketId}", new CreateTicketResult(ticketId));
        }).RequireAuthorization();
    }
}

public record CreateTicketRequest(string Subject, string Description);
public record CreateTicketResult(Guid TicketId);