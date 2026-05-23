using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Infrastructure.Data;
using SupportHub.TicketManagement.Infrastructure.Messaging;

namespace SupportHub.TicketManagement.Features.Events;

public record ProcessOrganizationCreatedCommand(OrganizationCreatedPayload Payload) : IRequest;

public class ProcessOrganizationCreatedHandler(
    TicketDbContext db,
    ILogger<ProcessOrganizationCreatedHandler> logger) : IRequestHandler<ProcessOrganizationCreatedCommand>
{
    private readonly TicketDbContext _db = db;
    private readonly ILogger<ProcessOrganizationCreatedHandler> _logger = logger;

    public async Task Handle(ProcessOrganizationCreatedCommand request, CancellationToken cancellationToken)
    {
        //Get the payload from the command
        OrganizationCreatedPayload payload = request.Payload;

        //Check if the organization snapshot already exists
        var existingOrganization = await _db.OrganizationSnapshots
            .FirstOrDefaultAsync(o => o.Id == payload.OrganizationId, cancellationToken);
        if (existingOrganization != null)
        {
            _logger.LogInformation("Organization snapshot with ID {OrganizationId} already exists. Skipping creation.", payload.OrganizationId);
            return;
        }

        //Create a new organization snapshot entry
        OrganizationSnapshot organizationSnapshot = new()
        {
            Id = payload.OrganizationId,
            SlaFirstResponseMinutes = 0.0m,
            SlaHumanResponseMinutes = 0.0m,
            SlaResolutionMinutes = 0.0m,
            AiTrustThreshold = 0.0m
        };
        _db.OrganizationSnapshots.Add(organizationSnapshot);

        //Create an AI user snapshot for the organization
        UserSnapshot aiUser = new()
        {
            Id = Guid.NewGuid(),
        };
        _db.UserSnapshots.Add(aiUser);

        //Create a user snapshot for the organization manager
        UserSnapshot managerUser = new()
        {
            Id = payload.ManagerId,
            Role = Role.ORGANIZATION_MANAGER,
            OrganizationId = payload.OrganizationId
        };
        _db.UserSnapshots.Add(managerUser);

        //Save all changes to the database
        await _db.SaveChangesAsync(cancellationToken);
    }
}