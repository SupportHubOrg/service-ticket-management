using FluentValidation;
using MediatR;

using SupportHub.TicketManagement.Domain.Enums;

namespace SupportHub.TicketManagement.Features.TicketMessages.GetTicketMessage;

/// <summary>
/// Query to retrieve a specific ticket's details.
/// </summary>
/// <param name="TicketMessageId">The ID of the ticket to retrieve.</param>
/// <param name="UserId">The ID of the user requesting the ticket, used for authorization.</param>
public record GetTicketMessageQuery(
    Guid TicketMessageId,
    Guid UserId
) : IRequest<TicketMessageDetailsResponse>;

/// <summary>
/// The read-model representation of a TicketMessage returned to the API client.
/// </summary>
public record TicketMessageDetailsResponse(
    Guid Id,
    Guid TicketId,
    Guid SenderId,
    Role SenderType,
    string Message,
    DateTime SentAt
);

public class GetTicketMessageValidator : AbstractValidator<GetTicketMessageQuery>
{
    public GetTicketMessageValidator()
    {
        RuleFor(x => x.TicketMessageId)
            .NotEmpty().WithMessage("TicketMessage ID is required.");
    }
}