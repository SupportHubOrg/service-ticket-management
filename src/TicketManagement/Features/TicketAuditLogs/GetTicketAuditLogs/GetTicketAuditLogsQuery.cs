using FluentValidation;
using MediatR;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.TicketAuditLogs.GetTicketAuditLogs;

/// <summary>
/// Query to retrieve a filtered, sorted, and paginated list of ticket audit logs.
/// </summary>
public record GetTicketAuditLogsQuery(
    Guid UserId,
    Guid? TicketId,
    Guid? ChangedById,
    Role? ActorType,
    string? Action,
    string? OldValue,
    string? NewValue,
    DateTime? CreatedAfter,
    DateTime? CreatedBefore,
    string? SortBy,
    Guid? OrganizationId,
    bool SortDescending = true,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<TicketAuditLogSummaryResponse>>;

/// <summary>
/// Representation returned in a list of TicketAuditLogs.
/// </summary>
public record TicketAuditLogSummaryResponse(
    Guid Id,
    Guid TicketId,
    Guid? ChangedById,
    Role ActorType,
    string Action,
    string? OldValue,
    string? NewValue,
    string Message,
    DateTime CreatedAt
);

public class GetTicketAuditLogsValidator : PagedQueryValidatorBase<GetTicketAuditLogsQuery, Ticket>
{
    public GetTicketAuditLogsValidator() : base(x => x.PageNumber, x => x.PageSize, x => x.SortBy)
    {
        RuleFor(x => x)
            .Must(x => !x.CreatedAfter.HasValue || !x.CreatedBefore.HasValue || x.CreatedAfter <= x.CreatedBefore)
            .WithMessage("CreatedAfter date cannot be later than CreatedBefore date.");

        RuleFor(x => x.ActorType)
            .IsInEnum()
            .When(x => x.ActorType.HasValue)
            .WithMessage("ActorType must be a valid Role enum value.");

        RuleFor(x => x.Action)
            .MaximumLength(255)
            .When(x => !string.IsNullOrEmpty(x.Action))
            .WithMessage("Action filter cannot be longer than 255 characters.");
        
        RuleFor(x => x.OldValue)
            .MaximumLength(255)
            .When(x => !string.IsNullOrEmpty(x.OldValue))
            .WithMessage("OldValue filter cannot be longer than 255 characters!");

        RuleFor(x => x.NewValue)
            .MaximumLength(255)
            .When(x => !string.IsNullOrEmpty(x.NewValue))
            .WithMessage("NewValue filter cannot be longer than 255 characters!");
    }
}