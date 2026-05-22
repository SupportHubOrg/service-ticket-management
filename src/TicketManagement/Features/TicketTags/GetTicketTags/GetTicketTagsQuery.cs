using FluentValidation;
using MediatR;

using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.TicketTags.GetTicketTags;

/// <summary>
/// Query to retrieve a filtered, sorted, and paginated list of tickets.
/// </summary>
public record GetTicketTagsQuery(
    Guid UserId,
    Guid? TagId,
    Guid? TicketId,
    Guid? AssignedById,
    string? SortBy,
    bool SortDescending = true,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<TicketTagSummaryResponse>>;

/// <summary>
/// A lightweight representation of a Ticket Tag used 
/// </summary>
public record TicketTagSummaryResponse(
    Guid TicketId,
    Guid TagId,
    Guid? AssignedById,
    string TagName,
    string Description,
    Guid OrganizationId,
    bool ExcludedFromAi
);
public class GetTicketTagsValidator : PagedQueryValidatorBase<GetTicketTagsQuery, Ticket>
{
    public GetTicketTagsValidator() : base(x => x.PageNumber, x => x.PageSize, x => x.SortBy)
    {
    }
}