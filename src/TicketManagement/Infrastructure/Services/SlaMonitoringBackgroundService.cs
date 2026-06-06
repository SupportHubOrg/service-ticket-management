using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Infrastructure.Data;
using SupportHub.TicketManagement.Infrastructure.Messaging;

namespace SupportHub.TicketManagement.Infrastructure.Services;

/// <summary>
/// A hosted background service that periodically checks for SLA breaches on open tickets.
/// Publishes events and updates tickets when breaches are detected.
/// </summary>
public class SlaMonitoringBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SlaMonitoringBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public SlaMonitoringBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SlaMonitoringBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// The main execution loop of the background service.
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SLA Monitoring Hosted Background Service has initialized successfully.");
        
        //Use a PeriodicTimer to trigger SLA checks at regular intervals
        using var timer = new PeriodicTimer(_checkInterval);

        //Every time the timer ticks, execute the SLA monitoring logic
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await MonitorDeadlinesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "A critical exception occurred during the execution of the cyclical SLA validation check.");
            }
        }
    }

    /// <summary>
    /// Performs the actual monitoring of SLA deadlines for all open tickets.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task MonitorDeadlinesAsync(CancellationToken cancellationToken)
    {
        //Get the required services from the dependency injection container
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        //Get the current UTC time to compare against SLA deadlines
        var now = DateTime.UtcNow;
        var approachingThreshold = now.AddMinutes(_checkInterval.TotalMinutes);

        //Get all tickets which are close to breaching their first response SLA deadline
        var firstResponseApproaching = await db.Tickets
            .Include(t => t.Creator)
            .Where(t => t.Status == Status.NEW && 
                        t.SlaFirstResponseDeadline.HasValue && 
                        t.SlaFirstResponseDeadline.Value > now &&
                        t.SlaFirstResponseDeadline.Value <= approachingThreshold)
            .ToListAsync(cancellationToken);

        //Iterate through each ticket that is approaching the first response SLA deadline
        foreach (var ticket in firstResponseApproaching)
        {
            //Calculate the minutes remaining until the SLA deadline is breached
            var minutesRemaining = (float)(ticket.SlaFirstResponseDeadline!.Value - now).TotalMinutes;

            //Create and publish an event indicating that the SLA deadline is approaching
            var approachingEvent = new SlaDeadlineApproachingEvent(
                ticket.Id,
                ticket.Creator.OrganizationId,
                SlaType.FIRST_RESPONSE,
                ticket.SlaFirstResponseDeadline.Value,
                minutesRemaining
            );
            await publisher.PublishAsync("SlaDeadlineApproaching", approachingEvent, cancellationToken);
        }

        //Get all tickets that have breached their first response SLA deadline
        var firstResponseBreaches = await db.Tickets
            .Include(t => t.Creator)
            .Where(t => t.Status == Status.NEW && 
                        t.SlaFirstResponseDeadline.HasValue && 
                        t.SlaFirstResponseDeadline.Value <= now)
            .ToListAsync(cancellationToken);

        //Iterate through each ticket that has breached the first response SLA
        foreach (var ticket in firstResponseBreaches)
        {
            //Update the ticket to indicate that it has breached the SLA
            ticket.Status = Status.UNASSIGNED;
            ticket.AssignedOperatorId = null;

            //Clear the SLA deadlines since the ticket has breached and is now unassigned
            ticket.SlaFirstResponseDeadline = null;
            ticket.SlaHumanResponseDeadline = null;
            ticket.SlaResolutionDeadline = null;

            ticket.UpdatedAt = now;

            //Create and publish an event indicating that the SLA deadline has been breached
            var breachEvent = new SlaDeadlineBreachedEvent(
                ticket.Id,
                ticket.Creator.OrganizationId,
                SlaType.FIRST_RESPONSE,
                ticket.SlaFirstResponseDeadline!.Value
            );
            await publisher.PublishAsync("SlaDeadlineBreached", breachEvent, cancellationToken);

            //Create and publish a ticket escalation event
            var escalationEvent = new TicketEscalatedEvent(
                ticket.Id,
                ticket.Creator.OrganizationId,
                Guid.Empty,
                Status.UNASSIGNED
            );
            await publisher.PublishAsync("TicketEscalated", escalationEvent, cancellationToken);
        }

        //Get all tickets that are close to breaching their human response SLA deadline
        var humanResponseApproaching = await db.Tickets
            .Include(t => t.Creator)
            .Where(t => (t.Status == Status.IN_PROGRESS_LEVEL_1 || t.Status == Status.IN_PROGRESS_LEVEL_2) && 
                        t.SlaHumanResponseDeadline.HasValue && 
                        t.SlaHumanResponseDeadline.Value > now &&
                        t.SlaHumanResponseDeadline.Value <= approachingThreshold)
            .ToListAsync(cancellationToken);

        //Iterate through each ticket that is approaching the human response SLA deadline
        foreach (var ticket in humanResponseApproaching)
        {
            //Calculate the minutes remaining until the SLA deadline is breached
            var minutesRemaining = (float)(ticket.SlaHumanResponseDeadline!.Value - now).TotalMinutes;

            //Create and publish an event indicating that the SLA deadline is approaching
            var approachingEvent = new SlaDeadlineApproachingEvent(
                ticket.Id,
                ticket.Creator.OrganizationId,
                SlaType.HUMAN_RESPONSE,
                ticket.SlaHumanResponseDeadline.Value,
                minutesRemaining
            );
            await publisher.PublishAsync("SlaDeadlineApproaching", approachingEvent, cancellationToken);
        }

        //Get all tickets that have breached their human response SLA deadline
        var humanResponseBreaches = await db.Tickets
            .Include(t => t.Creator)
            .Where(t => (t.Status == Status.IN_PROGRESS_LEVEL_1 || t.Status == Status.IN_PROGRESS_LEVEL_2) && 
                        t.SlaHumanResponseDeadline.HasValue && 
                        t.SlaHumanResponseDeadline.Value <= now)
            .ToListAsync(cancellationToken);

        //Iterate through each ticket that has breached the human response SLA
        foreach (var ticket in humanResponseBreaches)
        {
            //Save the old ticket status
            var oldStatus = ticket.Status;

            //Update the ticket to indicate that it has breached the SLA
            ticket.Status = Status.UNASSIGNED;
            ticket.AssignedOperatorId = null;

            //Clear the SLA deadlines since the ticket has breached and is now unassigned
            ticket.SlaHumanResponseDeadline = null;
            ticket.SlaResolutionDeadline = null;

            ticket.UpdatedAt = now;

            //Create and publish an event indicating that the SLA deadline has been breached
            var breachEvent = new SlaDeadlineBreachedEvent(
                ticket.Id,
                ticket.Creator.OrganizationId,
                SlaType.HUMAN_RESPONSE,
                ticket.SlaHumanResponseDeadline!.Value
            );
            await publisher.PublishAsync("SlaDeadlineBreached", breachEvent, cancellationToken);

            //Create and publish a ticket status change event to indicate that the ticket has been unassigned due to an SLA breach
            var statusChangeEvent = new TicketStatusChangedEvent(
                ticket.Id,
                ticket.Creator.OrganizationId,
                oldStatus,
                Status.UNASSIGNED,
                Guid.Empty,
                Role.SYSTEM
            );
            await publisher.PublishAsync("TicketStatusChanged", statusChangeEvent, cancellationToken);
        }

        //Get all tickets that are close to breaching their resolution SLA deadline
        var resolutionApproaching = await db.Tickets
            .Include(t => t.Creator)
            .Where(t => t.Status != Status.NOT_IN_KNOWLEDGE_BASE &&
                        t.Status != Status.IN_KNOWLEDGE_BASE && 
                        t.Status != Status.CLOSED &&
                        t.Status != Status.SOLUTION_IMPOSSIBLE &&
                        t.SlaResolutionDeadline.HasValue && 
                        t.SlaResolutionDeadline.Value > now &&
                        t.SlaResolutionDeadline.Value <= approachingThreshold)
            .ToListAsync(cancellationToken);

        //Iterate through each ticket that is approaching the resolution SLA deadline
        foreach (var ticket in resolutionApproaching)
        {
            //Calculate the minutes remaining until the SLA deadline is breached
            var minutesRemaining = (float)(ticket.SlaResolutionDeadline!.Value - now).TotalMinutes;

            //Create and publish an event indicating that the SLA deadline is approaching
            var approachingEvent = new SlaDeadlineApproachingEvent(
                ticket.Id,
                ticket.Creator.OrganizationId,
                SlaType.RESOLUTION,
                ticket.SlaResolutionDeadline.Value,
                minutesRemaining
            );
            await publisher.PublishAsync("SlaDeadlineApproaching", approachingEvent, cancellationToken);
        }

        //Get all tickets that have breached their resolution SLA deadline
        var resolutionBreaches = await db.Tickets
            .Include(t => t.Creator)
            .Where(t => t.Status != Status.NOT_IN_KNOWLEDGE_BASE &&
                        t.Status != Status.IN_KNOWLEDGE_BASE && 
                        t.Status != Status.CLOSED &&
                        t.Status != Status.SOLUTION_IMPOSSIBLE &&
                        t.SlaResolutionDeadline.HasValue && 
                        t.SlaResolutionDeadline.Value <= now)
            .ToListAsync(cancellationToken);

        foreach (var ticket in resolutionBreaches)
        {
            //Update the ticket to indicate that it has breached the SLA
            ticket.Status = Status.UNASSIGNED;
            ticket.AssignedOperatorId = null;

            //Clear the SLA deadlines since the ticket has breached and is now unassigned
            ticket.SlaHumanResponseDeadline = null;
            ticket.SlaResolutionDeadline = null;

            ticket.UpdatedAt = now;

            //Create and publish an event indicating that the SLA deadline has been breached
            var breachEvent = new SlaDeadlineBreachedEvent(
                ticket.Id,
                ticket.Creator.OrganizationId,
                SlaType.RESOLUTION,
                ticket.SlaResolutionDeadline!.Value
            );
            await publisher.PublishAsync("SlaDeadlineBreached", breachEvent, cancellationToken);

            //Create and publish a ticket status change event to indicate that the ticket has been unassigned due to an SLA breach
            var statusChangeEvent = new TicketStatusChangedEvent(
                ticket.Id,
                ticket.Creator.OrganizationId,
                ticket.Status,
                Status.UNASSIGNED,
                Guid.Empty,
                Role.SYSTEM
            );
            await publisher.PublishAsync("TicketStatusChanged", statusChangeEvent, cancellationToken);
        }

        //Save all changes to the database if there were any SLA breaches that required ticket updates
        if (firstResponseBreaches.Any() || humanResponseBreaches.Any() || resolutionBreaches.Any())
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}