using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Messaging;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.Tickets.CreateTicket;

public class CreateTicketHandler(TicketDbContext db, IEventPublisher publisher) : IRequestHandler<CreateTicketCommand, Guid>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;

    public async Task<Guid> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        //Retrieve the user and their organization to access SLA rules
        UserSnapshot? user = await _db.UserSnapshots
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        //Check if user exists and return 401 Unauthorized if not
        if (user == null)
            throw new AppException(StatusCodes.Status401Unauthorized, "User could not be found.");

        //Get the current time to set CreatedAt and calculate SLA deadlines
        var now = DateTime.UtcNow;

        //Get the AI user snapshot to use as the intial operator assignment for the ticket
        UserSnapshot? aiUser = await _db.UserSnapshots.Where(u => u.Role == Role.AI)
            .FirstOrDefaultAsync(cancellationToken);
        if (aiUser == null)
            throw new AppException(StatusCodes.Status500InternalServerError, "No AI user found in the database for initial ticket assignment.");

        //Create the new ticket and calculate SLA deadlines based on the organization's rules
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            CreatorId = request.UserId,
            AssignedOperatorId = aiUser.Id,
            Subject = request.Subject,
            Description = request.Description,
            Status = Status.NEW, 
            CreatedAt = now,
            UpdatedAt = now,
            SlaFirstResponseDeadline = now.AddMinutes((double)user.Organization.SlaFirstResponseMinutes),
            SlaHumanResponseDeadline = now.AddMinutes((double)user.Organization.SlaHumanResponseMinutes),
            SlaResolutionDeadline = now.AddMinutes((double)user.Organization.SlaResolutionMinutes)
        };

        //Save the ticket to the database
        _db.Tickets.Add(ticket);

        //Log the ticket creation
        TicketAuditLog ticketAuditLog = new()
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            ChangedById = request.UserId,
            ActorType = user.Role,
            Action = "Ticket Created",
            OldValue = null,
            NewValue = $"Subject: {ticket.Subject}, Description: {ticket.Description}, Status: {ticket.Status}",
            Message = $"Ticket {ticket.Id} created by user {request.UserId}",
            CreatedAt = now
        };
        _db.TicketAuditLogs.Add(ticketAuditLog);
        await _db.SaveChangesAsync(cancellationToken);

        //Publish the creation event to Kafka
        var ticketCreatedEvent = new TicketCreatedEvent(
            ticket.Id, 
            user.OrganizationId, 
            request.UserId, 
            ticket.Subject, 
            ticket.Description
        );
        await _publisher.PublishAsync("TicketCreated", ticketCreatedEvent, cancellationToken);

        //Return the assigned ID
        return ticket.Id;
    }
}