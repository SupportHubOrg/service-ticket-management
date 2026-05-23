using System.Security.Claims;
using MediatR;

namespace SupportHub.TicketManagement.Features.Tags.CreateTag;

public static class CreateTagEndpoint
{
    /// <summary>
    /// Maps the endpoint for creating a new Tag.
    /// </summary>
    /// <param name="app"></param>
    public static void MapCreateTagEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tags", async (
            CreateTagRequest request,
            HttpContext httpContext,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            //Extract user ID from JWT claims
            var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
                return Results.Unauthorized();

            //Create and send the command
            var command = new CreateTagCommand(userId, request.OrganizationId, request.Name, request.Description, request.ExcludedFromAi);
            var TagId = await mediator.Send(command);

            //Return 201 Created with the new resource route
            return Results.Created($"/api/Tags/{TagId}", new CreateTagResult(TagId));
        }).RequireAuthorization();
    }
}

public record CreateTagRequest(string Name, Guid OrganizationId, string Description, bool ExcludedFromAi);
public record CreateTagResult(Guid TagId);