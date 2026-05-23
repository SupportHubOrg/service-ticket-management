using System.Security.Claims;
using MediatR;

namespace SupportHub.TicketManagement.Features.TicketAuditLogs.DeleteTicketAuditLog;

public static class DeleteTicketAuditLogEndpoint
{
    /// <summary>
    /// Maps the endpoint for deleting a ticket audit log.
    /// </summary>
    public static void MapDeleteTicketAuditLogEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/ticket-audit-logs/{ticketId}", async (
            Guid ticketId,
            HttpContext httpContext,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            //Extract user ID from JWT claims
            var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
                return Results.Unauthorized();

            //Create and send the command to delete the ticket
            var command = new DeleteTicketAuditLogCommand(ticketId, userId);
            await mediator.Send(command);

            //Return success response
            return Results.NoContent();
        }).RequireAuthorization();
    }
}