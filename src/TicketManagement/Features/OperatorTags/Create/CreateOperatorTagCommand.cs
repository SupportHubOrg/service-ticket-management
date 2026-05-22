using FluentValidation;
using MediatR;

namespace SupportHub.TicketManagement.Features.OperatorTags.CreateOperatorTag;

/// <summary>
/// Command to create a new support ticket.
/// </summary>
/// <param name="TagId">ID of the tag to assign.</param>
/// <param name="OperatorId">ID of the operator to assign the tag to.</param>
public record CreateOperatorTagCommand(
    Guid UserId,
    Guid OperatorId,
    Guid TagId
) : IRequest<bool>;

public class CreateOperatorTagValidator : AbstractValidator<CreateOperatorTagCommand>
{
    public CreateOperatorTagValidator()
    {
        RuleFor(x => x.OperatorId)
            .NotEmpty().WithMessage("TicketId is required.");

        RuleFor(x => x.TagId)
            .NotEmpty().WithMessage("TagId is required.");
    }
}