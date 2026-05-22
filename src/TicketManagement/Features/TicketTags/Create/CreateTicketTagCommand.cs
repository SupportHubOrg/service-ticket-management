using FluentValidation;
using MediatR;

namespace SupportHub.TicketManagement.Features.TicketTags.CreateTicketTag;

/// <summary>
/// Command to create a new support ticket.
/// </summary>
/// <param name="AssignedById">ID of the user assigning the tag.</param>
/// <param name="TicketId">ID of the ticket to which the tag will be assigned.</param>
/// <param name="TagId">ID of the tag to assign.</param>
public record CreateTicketTagCommand(
    Guid AssignedById,
    Guid TicketId,
    Guid TagId
) : IRequest<bool>;

public class CreateTicketTagValidator : AbstractValidator<CreateTicketTagCommand>
{
    public CreateTicketTagValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty().WithMessage("TicketId is required.");

        RuleFor(x => x.TagId)
            .NotEmpty().WithMessage("TagId is required.");
    }
}