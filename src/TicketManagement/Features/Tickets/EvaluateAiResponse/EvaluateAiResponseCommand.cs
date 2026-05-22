using FluentValidation;
using MediatR;

namespace SupportHub.TicketManagement.Features.Tickets.EvaluateAiResponse;

/// <summary>
/// Command to submit user feedback and confirmation/denial of the usefulness of an AI-generated response for a specific ticket.
/// </summary>
/// <param name="TicketId"></param>
/// <param name="OrganizationId"></param>
/// <param name="EvaluatorId"></param>
/// <param name="IsHelpful"></param>
/// <param name="Feedback"></param>
public record EvaluateAiResponseCommand(
    Guid TicketId,
    Guid EvaluatorId,
    bool IsHelpful,
    string? Feedback
) : IRequest<bool>;

public class EvaluateAiResponseValidator : AbstractValidator<EvaluateAiResponseCommand>
{
    public EvaluateAiResponseValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty().WithMessage("Ticket ID is required.");

        RuleFor(x => x.IsHelpful)
            .NotNull().WithMessage("IsHelpful flag is required.");

        RuleFor(x => x.Feedback)
            .MaximumLength(1000).WithMessage("Feedback cannot exceed 1000 characters.");
    }
}