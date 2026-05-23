using FluentValidation;
using MediatR;

namespace SupportHub.TicketManagement.Features.Tags.UpdateTag;

/// <summary>
/// Allows updating a Tag's status and/or assigned operator.
/// </summary>
/// <param name="TagId">ID of the Tag to update.</param>
/// <param name="UserId">ID of the user performing the update.</param>
/// <param name="TargetExcludedFromAi">New value for whether the Tag should be excluded from AI processing.</param>
public record UpdateTagCommand(
    Guid UserId,
    Guid TagId,
    bool? TargetExcludedFromAi
) : IRequest<bool>;

public class UpdateTagValidator : AbstractValidator<UpdateTagCommand>
{
    public UpdateTagValidator()
    {
        RuleFor(x => x.TagId)
            .NotEmpty().WithMessage("Tag ID is required.");

        RuleFor(x => x.TargetExcludedFromAi)
            .NotNull().WithMessage("At least one field must be provided for update.");
    }
}