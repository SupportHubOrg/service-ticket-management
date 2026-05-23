using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Data;
using SupportHub.TicketManagement.Infrastructure.Messaging;

namespace SupportHub.TicketManagement.Features.TicketAuditLogs.DeleteTicketAuditLog;

public class DeleteTicketAuditLogHandler(TicketDbContext db, IEventPublisher publisher) : IRequestHandler<DeleteTicketAuditLogCommand, bool>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;

    public async Task<bool> Handle(DeleteTicketAuditLogCommand request, CancellationToken cancellationToken)
    {
        //Retrieve the ticket audit log to be deleted and check if it exists, otherwise return 404 Not Found
        TicketAuditLog? ticketAuditLog = await _db.TicketAuditLogs
            .Include(t => t.ChangedBy)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);
        if (ticketAuditLog == null)
            throw new AppException(StatusCodes.Status404NotFound, $"Ticket {request.TicketId} could not be found.");

        //Retrieve the user to verify permissions and check if they exist, otherwise return 401 Unauthorized
        UserSnapshot? user = await _db.UserSnapshots
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
            throw new AppException(StatusCodes.Status401Unauthorized, $"User {request.UserId} could not be found.");

        //Enforce strict authorization rules for deletion
        bool isSystemAdmin = user.Role == Role.SUPPORT_HUB_ADMINISTRATOR || user.Role == Role.SYSTEM;
        bool isOrganizationManager = user.Role == Role.ORGANIZATION_MANAGER && user.OrganizationId == ticketAuditLog.ChangedBy.OrganizationId;
        if (!isSystemAdmin && !isOrganizationManager)
            throw new AppException(StatusCodes.Status403Forbidden, $"User {request.UserId} attempted to delete ticket audit log {request.TicketId} without sufficient permissions. Only Organization Managers or SupportHub Administrators can delete tickets.");

        //Remove the ticket audit log from the database
        _db.TicketAuditLogs.Remove(ticketAuditLog);
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }
}