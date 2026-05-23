using FluentValidation;
using MediatR;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.TicketMessages.GetTicketMessages;

/// <summary>
/// Query to retrieve a filtered, sorted, and paginated list of tickets.
/// </summary>
public record GetTicketMessagesQuery(
    Guid UserId,
    Guid? TicketId,
    Guid? SenderId,
    Role? SenderType,
    DateTime? SentAfter,
    DateTime? SentBefore,
    string? SortBy,
    bool SortDescending = true,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PagedResult<TicketMessageSummaryResponse>>;

/// <summary>
/// Representation returned in a list of TicketMessages.
/// </summary>
public record TicketMessageSummaryResponse(
    Guid Id,
    Guid TicketId,
    Guid SenderId,
    Role SenderType,
    string Message,
    DateTime SentAt
);

public class GetTicketMessagesValidator : PagedQueryValidatorBase<GetTicketMessagesQuery, TicketMessage>
{
    public GetTicketMessagesValidator() : base(x => x.PageNumber, x => x.PageSize, x => x.SortBy)
    {
        RuleFor(x => x)
            .Must(x => !x.SentAfter.HasValue || !x.SentBefore.HasValue || x.SentAfter <= x.SentBefore)
            .WithMessage("SentAfter date cannot be later than SentBefore date.");
        
        RuleFor(x => x.SenderType)
            .IsInEnum()
            .When(x => x.SenderType.HasValue)
            .WithMessage("SenderType must be a valid role.");
    }
}