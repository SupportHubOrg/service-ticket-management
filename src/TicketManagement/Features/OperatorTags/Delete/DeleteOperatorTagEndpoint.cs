using System.Security.Claims;
using MediatR;

namespace SupportHub.TicketManagement.Features.OperatorTags.DeleteOperatorTag;

public static class DeleteOperatorTagEndpoint
{
    /// <summary>
    /// Maps the endpoint for deleting a tag from a support ticket.
    /// </summary>
    public static void MapDeleteOperatorTagEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/operator-tags/tags/{tagId}", async (
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
            var command = new DeleteOperatorTagCommand(ticketId, tagId, userId);
            await mediator.Send(command);

            //Return success response
            return Results.NoContent();
        }).RequireAuthorization();
    }
}