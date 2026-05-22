using FluentValidation;
using MediatR;

namespace SupportHub.TicketManagement.Features.Tags.GetTag;

/// <summary>
/// Query to retrieve a specific Tag's details.
/// </summary>
/// <param name="TagId">The ID of the Tag to retrieve.</param>
/// <param name="UserId">The ID of the user requesting the Tag, used for authorization.</param>
public record GetTagQuery(
    Guid TagId,
    Guid UserId
) : IRequest<TagDetailsResponse>;

/// <summary>
/// The read-model representation of a Tag returned to the API client.
/// </summary>
public record TagDetailsResponse(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string Description,
    bool ExcludedFromAi
);

public class GetTagValidator : AbstractValidator<GetTagQuery>
{
    public GetTagValidator()
    {
        RuleFor(x => x.TagId)
            .NotEmpty().WithMessage("Tag ID is required.");
    }
}