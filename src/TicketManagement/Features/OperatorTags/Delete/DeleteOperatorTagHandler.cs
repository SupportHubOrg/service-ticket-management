using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Data;
using SupportHub.TicketManagement.Infrastructure.Messaging;

namespace SupportHub.TicketManagement.Features.OperatorTags.DeleteOperatorTag;

public class DeleteOperatorTagHandler(TicketDbContext db, IEventPublisher publisher) : IRequestHandler<DeleteOperatorTagCommand, bool>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;

    public async Task<bool> Handle(DeleteOperatorTagCommand request, CancellationToken cancellationToken)
    {
        //Retrieve the operator tag to be deleted and check if it exists, return 404 Not Found if not
        OperatorTag? operatorTag = await _db.OperatorTags
            .Include(t => t.Operator)
            .Include(t => t.Tag)
            .FirstOrDefaultAsync(t => t.OperatorId == request.OperatorId && t.TagId == request.TagId, cancellationToken);
        if (operatorTag == null)
            throw new AppException(StatusCodes.Status404NotFound, $"Operator {request.OperatorId} Tag {request.TagId} could not be found.");

        //Retrieve the user and check if they exist, return 401 Unauthorized if not
        UserSnapshot? user = await _db.UserSnapshots
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
            throw new AppException(StatusCodes.Status401Unauthorized, $"User {request.UserId} could not be found.");

        //Check if the assignee is an administator or the organization manager, otherwise return 403 Forbidden
        bool isSystemAdmin = user.Role == Role.SUPPORT_HUB_ADMINISTRATOR || user.Role == Role.SYSTEM;
        bool isOrganizationManager = (user.Role == Role.ORGANIZATION_MANAGER || user.Role == Role.AI) && user.OrganizationId == operatorTag.Operator.OrganizationId;
        if (!isSystemAdmin && !isOrganizationManager)
            throw new AppException(StatusCodes.Status403Forbidden, $"User {request.UserId} attempted to delete tag {request.TagId} from operator {request.OperatorId} without sufficient permissions. Only the Organization Managers or SupportHub Administrators can remove tags from an operator.");

        //Remove the operator tag from the database
        _db.OperatorTags.Remove(operatorTag);
        await _db.SaveChangesAsync(cancellationToken);

        //Publish the tag update event to Kafka
        var operatorTagRemovedEvent = new OperatorTagRemovedEvent(operatorTag.OperatorId, operatorTag.TagId, request.UserId, operatorTag.Tag.Name); 
        await _publisher.PublishAsync("OperatorTagRemoved", operatorTagRemovedEvent, cancellationToken);

        return true;
    }
}