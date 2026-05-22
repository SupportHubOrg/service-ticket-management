using FluentValidation;
using MediatR;

namespace SupportHub.TicketManagement.Features.Tickets.ProposeArticle;

/// <summary>
/// Command to propose a new knowledge base article based on a ticket's resolution.
/// </summary>
/// <param name="TicketId">The ID of the ticket that sourced the knowledge.</param>
/// <param name="UserId">The ID of the user proposing the article.</param>
/// <param name="ProposedSubject">The suggested title for the knowledge article.</param>
/// <param name="Tags">An array of tags to categorize the article.</param>
public record ProposeArticleCommand(
    Guid TicketId,
    Guid UserId,
    string ProposedSubject,
    string[] Tags
) : IRequest<bool>;

public class ProposeArticleValidator : AbstractValidator<ProposeArticleCommand>
{
    public ProposeArticleValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty().WithMessage("Ticket ID is required.");

        RuleFor(x => x.ProposedSubject)
            .NotEmpty().WithMessage("A proposed subject is required.")
            .MaximumLength(200).WithMessage("Subject cannot exceed 200 characters.");

        RuleFor(x => x.Tags)
            .NotNull().WithMessage("Tags array cannot be null.");
    }
}