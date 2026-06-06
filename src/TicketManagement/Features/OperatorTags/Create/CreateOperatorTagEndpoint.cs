using System.Security.Claims;
using MediatR;

namespace SupportHub.TicketManagement.Features.OperatorTags.CreateOperatorTag;

public static class CreateOperatorTagEndpoint
{
    /// <summary>
    /// Maps the endpoint for creating a new support ticket.
    /// </summary>
    /// <param name="app"></param>
    public static void MapCreateOperatorTagEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tickets/tags/{tagId}/operators", async (
            Guid tagId,
            CreateOperatorTagRequest request,
            HttpContext httpContext,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            //Extract user ID from JWT claims
            var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
                return Results.Unauthorized();

            //Create and send the command
            var command = new CreateOperatorTagCommand(userId, request.OperatorId, tagId);
            await mediator.Send(command);

            //Return success response
            return Results.NoContent();
        }).RequireAuthorization();
    }
}

public record CreateOperatorTagRequest(Guid OperatorId);