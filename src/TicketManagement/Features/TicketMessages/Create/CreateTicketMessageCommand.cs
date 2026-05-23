using FluentValidation;
using MediatR;

namespace SupportHub.TicketManagement.Features.TicketMessages.CreateTicketMessage;

/// <summary>
/// Command to create a new support ticket.
/// </summary>
/// <param name="UserId">ID of the user creating the ticket.</param>
/// <param name="TicketId">ID of the ticket to which the message will be added.</param>
/// <param name="Message">Content of the ticket message.</param>
public record CreateTicketMessageCommand(
    Guid UserId,
    Guid TicketId,
    string Message
) : IRequest<Guid>;

public class CreateTicketMessageValidator : AbstractValidator<CreateTicketMessageCommand>
{
    public CreateTicketMessageValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Subject is required.")
            .MinimumLength(1).WithMessage("Subject must be at least 1 character long.")
            .MaximumLength(2000).WithMessage("Subject cannot be longer than 2000 characters.");

        RuleFor(x => x.TicketId)
            .NotEmpty().WithMessage("Ticket ID is required.");
    }
}