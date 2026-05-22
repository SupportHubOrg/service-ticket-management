using System.Security.Claims;
using MediatR;

namespace SupportHub.TicketManagement.Features.Tags.UpdateTag;

public static class UpdateTagEndpoint
{
    /// <summary>
    /// Maps the endpoint for updating a Tag's status and assigned operator.
    /// </summary>
    /// <param name="app"></param>
    public static void MapUpdateTagEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/Tags/{TagId}", async (
            Guid TagId,
            UpdateTagRequest request,
            HttpContext httpContext,
            ClaimsPrincipal user,
            IMediator mediator ) =>
        {
            //Extract user ID from JWT claims
            var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
                return Results.Unauthorized();

            //Create and send the command to update the Tag
            var command = new UpdateTagCommand(TagId, userId, request.TargetExcludedFromAi);
            await mediator.Send(command);

            //Return success response
            return Results.NoContent();
        }).RequireAuthorization();
    }
}

public record UpdateTagRequest(bool TargetExcludedFromAi);