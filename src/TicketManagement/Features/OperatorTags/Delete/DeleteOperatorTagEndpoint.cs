using System.Security.Claims;
using MediatR;

namespace SupportHub.TicketManagement.Features.OperatorTags.DeleteOperatorTag;

public static class DeleteOperatorTagEndpoint
{
    /// <summary>
    /// Maps the endpoint for deleting a tag from an operator.
    /// </summary>
    public static void MapDeleteOperatorTagEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/tickets/tags/{tagId}/operators/{operatorId}", async (
            Guid operatorId,
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
            var command = new DeleteOperatorTagCommand(operatorId, tagId, userId);
            await mediator.Send(command);

            //Return success response
            return Results.NoContent();
        }).RequireAuthorization();
    }
}