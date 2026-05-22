using MediatR;

using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.Tags.GetTags;

/// <summary>
/// Query to retrieve a filtered, sorted, and paginated list of Tags.
/// </summary>
public record GetTagsQuery(
    Guid UserId,
    Guid? OrganizationId,
    bool? ExcludedFromAi,
    string? SortBy,
    bool SortDescending = true,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<TagSummaryResponse>>;

/// <summary>
/// Representation returned in a list of Tags.
/// </summary>
public record TagSummaryResponse(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string Description,
    bool ExcludedFromAi
);

public class GetTagsValidator : PagedQueryValidatorBase<GetTagsQuery, Tag>
{
    public GetTagsValidator() : base(x => x.PageNumber, x => x.PageSize, x => x.SortBy)
    {
    }
}