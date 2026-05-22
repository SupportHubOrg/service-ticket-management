using FluentValidation;
using MediatR;

namespace SupportHub.TicketManagement.Features.TicketTags.DeleteTicketTag;

/// <summary>
/// Command to permanently delete a support ticket.
/// </summary>
/// <param name="TicketId">ID of the ticket from which the tag will be removed.</param>
/// <param name="TagId">ID of the tag to remove.</param>
/// <param name="UserId">ID of the user performing the deletion.</param>
public record DeleteTicketTagCommand(
    Guid TicketId, 
    Guid TagId,
    Guid UserId
) : IRequest<bool>;

public class DeleteTicketTagValidator : AbstractValidator<DeleteTicketTagCommand>
{
    public DeleteTicketTagValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty().WithMessage("Ticket ID is required.");
        
        RuleFor(x => x.TagId)
            .NotEmpty().WithMessage("Tag ID is required.");
    }
}