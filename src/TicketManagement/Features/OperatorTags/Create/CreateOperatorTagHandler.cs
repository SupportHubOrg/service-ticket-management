using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Messaging;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.OperatorTags.CreateOperatorTag;

public class CreateOperatorTagHandler(TicketDbContext db, IEventPublisher publisher) : IRequestHandler<CreateOperatorTagCommand, bool>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;

    public async Task<bool> Handle(CreateOperatorTagCommand request, CancellationToken cancellationToken)
    {
        //Retrieve the user and check if they exist, if not return 401 Unauthorized
        UserSnapshot? user = await _db.UserSnapshots
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
            throw new AppException(StatusCodes.Status401Unauthorized, "User could not be found.");

        //Get the operator and check if they exists, otherwise return 404 Not Found
        UserSnapshot? operatorUser = await _db.UserSnapshots
            .FirstOrDefaultAsync(u => u.Id == request.OperatorId, cancellationToken);
        if (operatorUser == null)
            throw new AppException(StatusCodes.Status404NotFound, $"Operator {request.OperatorId} could not be found.");
        //Check if the operator is actually an operator, otherwise return 400 Bad Request
        if (operatorUser.Role != Role.LEVEL_1_OPERATOR && operatorUser.Role != Role.LEVEL_2_OPERATOR)
            throw new AppException(StatusCodes.Status400BadRequest, $"User {request.OperatorId} is not an operator.");

        //Get the tag and check if it exists, otherwise return 404 Not Found
        Tag? tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == request.TagId, cancellationToken);
        if (tag == null)
            throw new AppException(StatusCodes.Status404NotFound, $"Tag {request.TagId} could not be found.");
        
        //Check if the operator does not already have the tag, if they do return 400 Bad Request
        bool alreadyHasTag = await _db.OperatorTags.AnyAsync(ot => ot.OperatorId == request.OperatorId && ot.TagId == request.TagId, cancellationToken);
        if (alreadyHasTag)
            throw new AppException(StatusCodes.Status400BadRequest, $"Operator {request.OperatorId} already has tag {request.TagId}.");

        //Check if the assignee is the administator or the organization manager, otherwise return 403 Forbidden
        bool isSystemAdmin = user.Role == Role.SUPPORT_HUB_ADMINISTRATOR || user.Role == Role.SYSTEM;
        bool isOrganizationManager = (user.Role == Role.ORGANIZATION_MANAGER || user.Role == Role.AI) && user.OrganizationId == operatorUser.OrganizationId && user.OrganizationId == tag.OrganizationId;
        if (!isSystemAdmin && !isOrganizationManager)
            throw new AppException(StatusCodes.Status403Forbidden, $"User {request.UserId} attempted to add tag {request.TagId} to operator {request.OperatorId} without sufficient permissions. Only the Organization Managers within their organization or SupportHub Administrators can add tags to a ticket.");

        //Create the new operator tag
        var OperatorTag = new OperatorTag
        {
            OperatorId = request.OperatorId,
            TagId = request.TagId
        };

        //Save the ticket tag to the database
        _db.OperatorTags.Add(OperatorTag);
        await _db.SaveChangesAsync(cancellationToken);

        //Publish the tag added event to Kafka
        var operatorTagAddedEvent = new OperatorTagAddedEvent(request.OperatorId, request.TagId, request.UserId, tag.Name); 
        await _publisher.PublishAsync("OperatorTagAddedEvent", operatorTagAddedEvent, cancellationToken);

        return true;
    }
}