using FluentValidation;
using MediatR;

namespace SupportHub.TicketManagement.Features.Tickets.CreateTicket;

/// <summary>
/// Command to create a new support ticket.
/// </summary>
/// <param name="UserId">ID of the user creating the ticket.</param>
/// <param name="Subject">A brief summary of the issue.</param>
/// <param name="Description">Detailed explanation of the problem.</param>
public record CreateTicketCommand(
    Guid UserId,
    string Subject,
    string Description
) : IRequest<Guid>;

public class CreateTicketValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketValidator()
    {
        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Subject is required.")
            .MinimumLength(5).WithMessage("Subject must be at least 5 characters long.")
            .MaximumLength(200).WithMessage("Subject cannot be longer than 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MinimumLength(15).WithMessage("Description must be at least 15 characters long.")
            .MaximumLength(10000).WithMessage("Description cannot be longer than 10000 characters.");
    }
}