using FluentValidation;
using MediatR;

namespace SupportHub.TicketManagement.Features.TicketAuditLogs.DeleteTicketAuditLog;

/// <summary>
/// Command to permanently delete a support ticket.
/// </summary>
/// <param name="TicketId">ID of the ticket to delete.</param>
/// <param name="UserId">ID of the user attempting to perform the deletion.</param>
public record DeleteTicketAuditLogCommand(
    Guid TicketId, 
    Guid UserId
) : IRequest<bool>;

public class DeleteTicketAuditLogValidator : AbstractValidator<DeleteTicketAuditLogCommand>
{
    public DeleteTicketAuditLogValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty().WithMessage("Ticket ID is required.");
    }
}