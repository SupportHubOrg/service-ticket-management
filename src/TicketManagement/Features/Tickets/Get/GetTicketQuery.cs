using FluentValidation;
using MediatR;
using SupportHub.TicketManagement.Domain.Enums;

namespace SupportHub.TicketManagement.Features.Tickets.GetTicket;

/// <summary>
/// Query to retrieve a specific ticket's details.
/// </summary>
/// <param name="TicketId">The ID of the ticket to retrieve.</param>
/// <param name="UserId">The ID of the user requesting the ticket, used for authorization.</param>
public record GetTicketQuery(
    Guid TicketId,
    Guid UserId
) : IRequest<TicketDetailsResponse>;

/// <summary>
/// The read-model representation of a Ticket returned to the API client.
/// </summary>
public record TicketDetailsResponse(
    Guid Id,
    string Subject,
    string Description,
    Status Status,
    Guid CreatorId,
    Guid? AssignedOperatorId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? SlaFirstResponseDeadline,
    DateTime? SlaHumanResponseDeadline,
    DateTime? SlaResolutionDeadline
);

public class GetTicketValidator : AbstractValidator<GetTicketQuery>
{
    public GetTicketValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty().WithMessage("Ticket ID is required.");
    }
}