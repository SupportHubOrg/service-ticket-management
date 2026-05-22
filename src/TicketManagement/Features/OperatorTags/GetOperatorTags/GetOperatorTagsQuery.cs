using MediatR;

using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.OperatorTags.GetOperatorTags;

/// <summary>
/// Query to retrieve a filtered, sorted, and paginated list of ticket tags.
/// </summary>
public record GetOperatorTagsQuery(
    Guid UserId,
    Guid? TagId,
    Guid? OperatorId,
    string? SortBy,
    bool SortDescending = true,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<OperatorTagSummaryResponse>>;

/// <summary>
/// A lightweight representation of a Ticket Tag used 
/// </summary>
public record OperatorTagSummaryResponse(
    Guid TagId,
    Guid OperatorId,
    string TagName,
    string Description,
    Guid OrganizationId,
    bool ExcludedFromAi
);
public class GetOperatorTagsValidator : PagedQueryValidatorBase<GetOperatorTagsQuery, Ticket>
{
    public GetOperatorTagsValidator() : base(x => x.PageNumber, x => x.PageSize, x => x.SortBy)
    {
    }
}