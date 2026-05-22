using System.Security.Claims;
using MediatR;

namespace SupportHub.TicketManagement.Features.TicketTags.CreateTicketTag;

public static class CreateTicketTagEndpoint
{
    /// <summary>
    /// Maps the endpoint for creating a new support ticket.
    /// </summary>
    /// <param name="app"></param>
    public static void MapCreateTicketTagEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tickets/{ticketId}/tags/{tagId}", async (
            Guid ticketId,
            Guid tagId,
            HttpContext httpContext,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            //Extract user ID from JWT claims
            var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
                return Results.Unauthorized();

            //Create and send the command
            var command = new CreateTicketTagCommand(userId, ticketId, tagId);
            await mediator.Send(command);

            //Return success response
            return Results.NoContent();
        }).RequireAuthorization();
    }
}