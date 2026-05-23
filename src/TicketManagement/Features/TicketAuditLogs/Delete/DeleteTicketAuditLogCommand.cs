using FluentValidation;
using MediatR;

namespace SupportHub.TicketManagement.Features.TicketAuditLogs.DeleteTicketAuditLog;

/// <summary>
/// Command to permanently delete a support ticket.
/// </summary>
/// <param name="TicketAuditLogId">ID of the ticket audit log to be deleted.</param>
/// <param name="UserId">ID of the user attempting to perform the deletion.</param>
public record DeleteTicketAuditLogCommand(
    Guid TicketAuditLogId, 
    Guid UserId
) : IRequest<bool>;

public class DeleteTicketAuditLogValidator : AbstractValidator<DeleteTicketAuditLogCommand>
{
    public DeleteTicketAuditLogValidator()
    {
        RuleFor(x => x.TicketAuditLogId)
            .NotEmpty().WithMessage("Ticket ID is required.");
    }
}