using System.Security.Claims;
using MediatR;

namespace SupportHub.TicketManagement.Features.TicketTags.DeleteTicketTag;

public static class DeleteTicketTagEndpoint
{
    /// <summary>
    /// Maps the endpoint for deleting a tag from a support ticket.
    /// </summary>
    public static void MapDeleteTicketTagEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/tickets/{ticketId}/tags/{TagId}", async (
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

            //Create and send the command to delete the ticket tag
            var command = new DeleteTicketTagCommand(ticketId, tagId, userId);
            await mediator.Send(command);

            //Return success response
            return Results.NoContent();
        }).RequireAuthorization();
    }
}