using FluentValidation;
using MediatR;

namespace SupportHub.TicketManagement.Features.OperatorTags.DeleteOperatorTag;

/// <summary>
/// Command to permanently delete a support ticket.
/// </summary>
/// <param name="OperatorId">ID of the operator from whom the tag should be removed.</param>
/// <param name="TagId">ID of the tag to remove.</param>
/// <param name="UserId">ID of the user performing the deletion.</param>
public record DeleteOperatorTagCommand(
    Guid OperatorId, 
    Guid TagId,
    Guid UserId
) : IRequest<bool>;

public class DeleteOperatorTagValidator : AbstractValidator<DeleteOperatorTagCommand>
{
    public DeleteOperatorTagValidator()
    {
        RuleFor(x => x.OperatorId)
            .NotEmpty().WithMessage("Operator ID is required.");
        
        RuleFor(x => x.TagId)
            .NotEmpty().WithMessage("Tag ID is required.");
    }
}