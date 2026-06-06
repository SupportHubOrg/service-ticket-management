using FluentValidation;
using MediatR;

using SupportHub.TicketManagement.Domain.Enums;

namespace SupportHub.TicketManagement.Features.Tickets.UpdateTicket;

/// <summary>
/// Allows updating a ticket's status and/or assigned operator.
/// </summary>
/// <param name="TicketId">ID of the ticket to update.</param>
/// <param name="UserId">ID of the user performing the update.</param>
/// <param name="Status">Status to update the ticket to. Must be a valid enum value.</param>
/// <param name="OperatorId">ID of the operator to assign the ticket to. Optional; if null, the ticket will be unassigned.</param>
public record UpdateTicketCommand(
    Guid UserId,
    Guid TicketId,
    Status Status,
    Guid? OperatorId
) : IRequest<bool>;

public class UpdateTicketValidator : AbstractValidator<UpdateTicketCommand>
{

    public UpdateTicketValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Target status is required.")
            .IsInEnum().WithMessage("The provided status does not exist.");

        RuleFor(x => x.OperatorId)
            .Must(id => id == null || id != Guid.Empty).WithMessage("Target operator ID must be a valid GUID or null.");

        RuleFor(x => x.TicketId)
            .NotEmpty().WithMessage("Ticket ID is required.");
    }
}