using System.Security.Claims;
using MediatR;

namespace SupportHub.TicketManagement.Features.Tickets.EvaluateAiResponse;

public static class EvaluateAiResponseEndpoint
{
    /// <summary>
    /// Maps the endpoint for evaluating whether the Ai reponse helped resolve the ticket.
    /// </summary>
    public static void MapEvaluateAiResponseEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tickets/{ticketId}/evaluate-ai-response", async (
            Guid ticketId,
            EvaluateAiResponseRequest request,
            HttpContext httpContext,
            ClaimsPrincipal claimsPrincipal,
            IMediator mediator) =>
        {
            //Extract user ID from JWT claims
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
                return Results.Unauthorized();

            //Create and send the command
            var command = new EvaluateAiResponseCommand(ticketId, userId, request.IsHelpful, request.Feedback);
            await mediator.Send(command);

            //Return 202 Accepted to indicate the feedback was received
            return Results.Accepted();
        }).RequireAuthorization();
    }
}

public record EvaluateAiResponseRequest(bool IsHelpful, string Feedback);