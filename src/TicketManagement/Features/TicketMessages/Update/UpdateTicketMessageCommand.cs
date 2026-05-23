using FluentValidation;
using MediatR;

using SupportHub.TicketManagement.Domain.Enums;

namespace SupportHub.TicketManagement.Features.TicketMessages.UpdateTicketMessage;

/// <summary>
/// Allows updating a ticket's status and/or assigned operator.
/// </summary>
/// <param name="TicketMessageId">ID of the ticket to update.</param>
/// <param name="UserId">ID of the user performing the update.</param>
/// <param name="Message">The new message content to update.</param>
public record UpdateTicketMessageCommand(
    Guid UserId,
    Guid TicketMessageId,
    string Message
) : IRequest<bool>;

public class UpdateTicketMessageValidator : AbstractValidator<UpdateTicketMessageCommand>
{
    public UpdateTicketMessageValidator()
    {
        RuleFor(x => x.TicketMessageId)
            .NotEmpty().WithMessage("TicketMessage ID is required.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(2000).WithMessage("Message cannot be longer than 2000 characters.")
            .MinimumLength(1).WithMessage("Message must be at least 1 character long.");
    }
}