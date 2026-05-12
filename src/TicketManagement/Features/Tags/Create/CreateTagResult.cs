namespace SupportHub.TicketManagement.Features.Tags.Create;

public record CreateTagResult(
    Guid Id,
    string Name,
    DateTime CreatedAt
);