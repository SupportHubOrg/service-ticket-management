using FluentValidation;
using MediatR;

namespace SupportHub.TicketManagement.Features.Tags.CreateTag;

/// <summary>
/// Command to create a new support ticket.
/// </summary>
/// <param name="UserId">ID of the user creating the ticket.</param>
/// <param name="Name">Name of the tag.</param>
/// <param name="Description">Description of the tag.</param>
/// <param name="ExcludedFromAi">Whether the tag should be excluded from AI processing.</param>
public record CreateTagCommand(
    Guid UserId,
    Guid OrganizationId,
    string Name,
    string Description,
    bool ExcludedFromAi
) : IRequest<Guid>;

public class CreateTagValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MinimumLength(5).WithMessage("Name must be at least 5 characters long.")
            .MaximumLength(200).WithMessage("Name cannot be longer than 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MinimumLength(15).WithMessage("Description must be at least 15 characters long.")
            .MaximumLength(10000).WithMessage("Description cannot be longer than 10000 characters.");

        RuleFor(x => x.ExcludedFromAi)
            .NotNull().WithMessage("ExcludedFromAi must be specified.");

        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("Organization ID is required.");
    }
}