using FluentValidation;
using MediatR;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.Tickets.GetTickets;

/// <summary>
/// Query to retrieve a filtered, sorted, and paginated list of tickets.
/// </summary>
public record GetTicketsQuery(
    Guid UserId,
    Guid? OrganizationId,
    Guid? CreatorId,
    Guid? AssignedOperatorId,
    DateTime? CreatedAfter,
    DateTime? CreatedBefore,
    string? SortBy,
    bool SortDescending = true,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<TicketSummaryResponse>>;

/// <summary>
/// A lightweight representation of a Ticket used for list views and dashboards.
/// </summary>
public record TicketSummaryResponse(
    Guid Id,
    string Subject,
    Status Status,
    Guid CreatorId,
    Guid? AssignedOperatorId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public class GetTicketsValidator : PagedQueryValidatorBase<GetTicketsQuery, Ticket>
{
    public GetTicketsValidator() : base(x => x.PageNumber, x => x.PageSize, x => x.SortBy)
    {
        RuleFor(x => x)
            .Must(x => !x.CreatedAfter.HasValue || !x.CreatedBefore.HasValue || x.CreatedAfter <= x.CreatedBefore)
            .WithMessage("CreatedAfter date cannot be later than CreatedBefore date.");
    }
}