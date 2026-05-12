using MediatR;

namespace SupportHub.TicketManagement.Features.Tags.Create;

public record CreateTagCommand(
    Guid OrganizationId,
    string Name,
    string Description,
    bool ExcludedFromAi
) : IRequest<CreateTagResult>;