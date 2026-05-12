using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Security.Claims;

namespace SupportHub.TicketManagement.Features.Tags.Create;

public static class CreateTagEndpoint
{
    public static void MapCreateTagEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/organizations/{orgId}/tags", async (
            Guid orgId,
            CreateTagCommand command,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            if (orgId != command.OrganizationId)
                return Results.BadRequest("Organization ID mismatch.");

            var result = await mediator.Send(command);

            return Results.Created($"/api/organizations/{orgId}/tags/{result.Id}", result);
        })
        .WithName("CreateTag")
        .WithTags("Tags")
        .RequireAuthorization();
    }
}