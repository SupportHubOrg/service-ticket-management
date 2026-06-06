using System.Security.Claims;
using MediatR;

namespace SupportHub.TicketManagement.Features.Tags.DeleteTag;

public static class DeleteTagEndpoint
{
    /// <summary>
    /// Maps the endpoint for deleting a Tag.
    /// </summary>
    public static void MapDeleteTagEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/tickets/tags/{tagId}", async (
            Guid tagId,
            HttpContext httpContext,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            //Extract user ID from JWT claims
            var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
                return Results.Unauthorized();

            //Create and send the command to delete the Tag
            var command = new DeleteTagCommand(tagId, userId);
            await mediator.Send(command);

            //Return success response
            return Results.NoContent();
        }).RequireAuthorization();
    }
}