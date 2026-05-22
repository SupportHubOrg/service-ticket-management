using System.Security.Claims;
using MediatR;

namespace SupportHub.TicketManagement.Features.Tickets.ProposeArticle;

public static class ProposeArticleEndpoint
{
    /// <summary>
    /// Maps the endpoint for proposing a knowledge base article from a ticket.
    /// </summary>
    public static void MapProposeArticleEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tickets/{ticketId}/propose-article", async (
            Guid ticketId,
            ProposeArticleRequest request,
            HttpContext httpContext,
            ClaimsPrincipal claimsPrincipal,
            IMediator mediator) =>
        {
            //Extract user ID from JWT claims
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
                return Results.Unauthorized();

            //Create and send the command
            var command = new ProposeArticleCommand(ticketId, userId, request.ProposedSubject, request.Tags);
            await mediator.Send(command);

            //Return 202 Accepted to indicate the proposal was received and is being processed
            return Results.Accepted();
        }).RequireAuthorization();
    }
}

public record ProposeArticleRequest(string ProposedSubject, string[] Tags);