using FluentValidation;
using MediatR;

namespace SupportHub.TicketManagement.Features.Tickets.DeleteTicket;

/// <summary>
/// Command to permanently delete a support ticket.
/// </summary>
/// <param name="TicketId">ID of the ticket to delete.</param>
/// <param name="UserId">ID of the user attempting to perform the deletion.</param>
public record DeleteTicketCommand(
    Guid TicketId, 
    Guid UserId
) : IRequest<bool>;

public class DeleteTicketValidator : AbstractValidator<DeleteTicketCommand>
{
    public DeleteTicketValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty().WithMessage("Ticket ID is required.");
    }
}