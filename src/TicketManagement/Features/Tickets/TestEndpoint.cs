using System.Security.Claims;

using SupportHub.TicketManagement.Infrastructure.Messaging;

namespace SupportHub.TicketManagement.Features.Tickets.Test;

public static class TestEndpoint
{
    public static void MapTestEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/test", async (
            [AsParameters] TestRequest parameters,
            HttpContext httpContext,
            ClaimsPrincipal claimsPrincipal,
            IEventPublisher eventPublisher) =>
        {
            var ticketEvent = new TicketCreatedEvent(
                Guid.NewGuid(), 
                Guid.NewGuid(), 
                Guid.NewGuid(), 
                "Test Ticket", 
                "This is a test ticket created to verify the event bus integration."
            );

            //Publish a test TicketCreatedEvent to verify that the event bus is working end-to-end
            await eventPublisher.PublishAsync("TicketCreated", ticketEvent);

            //Return a simple success message
            return Results.Ok("Test ticket creation event published successfully.");
        });
    }
}

public record TestRequest(
    Guid? testId
);