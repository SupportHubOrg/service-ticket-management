using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Infrastructure.Data;
using SupportHub.TicketManagement.Infrastructure.Messaging;

namespace SupportHub.TicketManagement.Features.Events;

public record ProcessOrganizationSettingsUpdatedCommand(OrganizationSettingsUpdatedPayload Payload) : IRequest;

public class ProcessOrganizationSettingsUpdatedHandler(
    TicketDbContext db,
    ILogger<ProcessOrganizationSettingsUpdatedHandler> logger) : IRequestHandler<ProcessOrganizationSettingsUpdatedCommand>
{
    private readonly TicketDbContext _db = db;
    private readonly ILogger<ProcessOrganizationSettingsUpdatedHandler> _logger = logger;

    public async Task Handle(ProcessOrganizationSettingsUpdatedCommand request, CancellationToken cancellationToken)
    {
        //Get the payload from the command
        OrganizationSettingsUpdatedPayload payload = request.Payload;

        //Fetch the organization snapshot from the database
        var organizationSnapshot = await _db.OrganizationSnapshots
            .FirstOrDefaultAsync(o => o.Id == payload.OrganizationId, cancellationToken);
        
        if (organizationSnapshot == null)
        {
            _logger.LogWarning("Organization snapshot with ID {OrganizationId} not found for settings update processing.", payload.OrganizationId);
            return;
        }

        //Save previous settings for logging
        decimal previousAiTrustThreshold = organizationSnapshot.AiTrustThreshold;
        decimal previousSlaFirstResponseMinutes = organizationSnapshot.SlaFirstResponseMinutes;
        decimal previousSlaHumanResponseMinutes = organizationSnapshot.SlaHumanResponseMinutes;
        decimal previousSlaResolutionMinutes = organizationSnapshot.SlaResolutionMinutes;

        //Update the organization settings
        organizationSnapshot.AiTrustThreshold = payload.AiTrustThreshold;
        organizationSnapshot.SlaFirstResponseMinutes = payload.SlaFirstResponseMinutes;
        organizationSnapshot.SlaHumanResponseMinutes = payload.SlaHumanResponseMinutes;
        organizationSnapshot.SlaResolutionMinutes = payload.SlaResolutionMinutes;

        //Save the changes to the database
        await _db.SaveChangesAsync(cancellationToken);
    }
}