using FluentValidation;
using MediatR;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Enums;

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

/// <summary>
/// Standardized wrapper for paginated responses.
/// </summary>
public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
);

public class GetTicketsValidator : AbstractValidator<GetTicketsQuery>
{
    public GetTicketsValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
            
        RuleFor(x => x)
            .Must(x => !x.CreatedAfter.HasValue || !x.CreatedBefore.HasValue || x.CreatedAfter <= x.CreatedBefore)
            .WithMessage("CreatedAfter date cannot be later than CreatedBefore date.");

        RuleFor(x => x.SortBy)
            .Must(sortBy => string.IsNullOrEmpty(sortBy) || typeof(Ticket).GetProperty(sortBy) != null)
            .WithMessage("SortBy must be a valid property name of Ticket.");
    }
}