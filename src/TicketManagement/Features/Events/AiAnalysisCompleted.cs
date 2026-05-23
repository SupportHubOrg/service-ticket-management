using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Infrastructure.Data;
using SupportHub.TicketManagement.Infrastructure.Messaging;

namespace SupportHub.TicketManagement.Features.Events;

public record ProcessAiAnalysisCompletedCommand(Guid TicketId) : IRequest;

public class ProcessAiAnalysisCompletedHandler(TicketDbContext db, IEventPublisher publisher) : IRequestHandler<ProcessAiAnalysisCompletedCommand>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;

    public async Task Handle(ProcessAiAnalysisCompletedCommand request, CancellationToken cancellationToken)
    {
        //Retrieve the AI user snapshot to use as the sender of the AI message
        UserSnapshot? aiUser = await _db.UserSnapshots.Where(u => u.Role == Role.AI)
            .FirstOrDefaultAsync(cancellationToken);

        //Add a new message to the ticket with the AI analysis results
        TicketMessage aiMessage = new()
        {
            Id = Guid.NewGuid(),
            TicketId = request.TicketId,
            SenderId = Guid.Empty,
            SenderType = Role.AI,
            Message = "AI analysis has been completed for this ticket. Please review the AI-generated insights and recommendations.",
            SentAt = DateTime.UtcNow
        };
    }
}