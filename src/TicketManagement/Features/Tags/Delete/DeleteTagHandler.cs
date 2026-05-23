using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Data;
using SupportHub.TicketManagement.Infrastructure.Messaging;

namespace SupportHub.TicketManagement.Features.Tags.DeleteTag;

public class DeleteTagHandler(TicketDbContext db, IEventPublisher publisher) : IRequestHandler<DeleteTagCommand, bool>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;

    public async Task<bool> Handle(DeleteTagCommand request, CancellationToken cancellationToken)
    {
        //Retrieve the Tag to be deleted and check if it exists, otherwise return 404 Not Found
        Tag? Tag = await _db.Tags
            .FirstOrDefaultAsync(t => t.Id == request.TagId, cancellationToken);
        if (Tag == null)
            throw new AppException(StatusCodes.Status404NotFound, $"Tag {request.TagId} could not be found.");

        //Retrieve the user to verify permissions and check if they exist, otherwise return 401 Unauthorized
        UserSnapshot? user = await _db.UserSnapshots
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
            throw new AppException(StatusCodes.Status401Unauthorized, $"User {request.UserId} could not be found.");

        //Enforce strict authorization rules for deletion
        bool isSystemAdmin = user.Role == Role.SUPPORT_HUB_ADMINISTRATOR || user.Role == Role.SYSTEM;
        bool isOrganizationManager = user.Role == Role.ORGANIZATION_MANAGER && user.OrganizationId == Tag.OrganizationId;
        if (!isSystemAdmin && !isOrganizationManager)
            throw new AppException(StatusCodes.Status403Forbidden, $"User {request.UserId} attempted to delete Tag {request.TagId} without sufficient permissions. Only Organization Managers or SupportHub Administrators can delete Tags.");

        //Remove the Tag from the database
        _db.Tags.Remove(Tag);
        await _db.SaveChangesAsync(cancellationToken);

        //Publish an event to notify other services that a Tag has been deleted
        TagDeletedEvent tagDeletedEvent = new(Tag.Id, Tag.OrganizationId, request.UserId);
        await _publisher.PublishAsync("TagDeleted", tagDeletedEvent, cancellationToken);

        return true;
    }
}