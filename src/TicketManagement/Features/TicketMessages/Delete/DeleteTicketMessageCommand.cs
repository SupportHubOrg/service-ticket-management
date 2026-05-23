using FluentValidation;
using MediatR;

namespace SupportHub.TicketManagement.Features.TicketMessages.DeleteTicketMessage;

/// <summary>
/// Command to permanently delete a support ticket.
/// </summary>
/// <param name="TicketMessageId">ID of the ticket to delete.</param>
/// <param name="UserId">ID of the user attempting to perform the deletion.</param>
public record DeleteTicketMessageCommand(
    Guid TicketMessageId, 
    Guid UserId
) : IRequest<bool>;

public class DeleteTicketMessageValidator : AbstractValidator<DeleteTicketMessageCommand>
{
    public DeleteTicketMessageValidator()
    {
        RuleFor(x => x.TicketMessageId)
            .NotEmpty().WithMessage("TicketMessage ID is required.");
    }
}