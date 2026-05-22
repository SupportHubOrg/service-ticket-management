using FluentValidation;
using MediatR;

namespace SupportHub.TicketManagement.Features.Tags.DeleteTag;

/// <summary>
/// Command to permanently delete a tag.
/// </summary>
/// <param name="TagId">ID of the Tag to delete.</param>
/// <param name="UserId">ID of the user attempting to perform the deletion.</param>
public record DeleteTagCommand(
    Guid TagId,
    Guid UserId
) : IRequest<bool>;

public class DeleteTagValidator : AbstractValidator<DeleteTagCommand>
{
    public DeleteTagValidator()
    {
        RuleFor(x => x.TagId)
            .NotEmpty().WithMessage("Tag ID is required.");
    }
}