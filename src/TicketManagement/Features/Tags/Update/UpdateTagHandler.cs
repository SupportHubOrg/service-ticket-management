using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Messaging;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.Tags.UpdateTag;

public class UpdateTagHandler(TicketDbContext db, IEventPublisher publisher) : IRequestHandler<UpdateTagCommand, bool>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;

    public async Task<bool> Handle(UpdateTagCommand request, CancellationToken cancellationToken)
    {
        //Retrieve the Tag to be updated and check if it exists, otherwise return 404 Not Found
        Tag? Tag = await _db.Tags
            .FirstOrDefaultAsync(t => t.Id == request.TagId, cancellationToken);
        if (Tag == null)
            throw new AppException(StatusCodes.Status404NotFound, $"Tag {request.TagId} could not be found.");

        //Check if the user exists and return 401 Unauthorized if not
        var user = await _db.UserSnapshots.Where(u => u.Id == request.UserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (user == null)
            throw new AppException(StatusCodes.Status401Unauthorized, "User could not be found.");

        //Enforce strict authorization rules
        bool isSystemAdmin = user.Role == Role.SUPPORT_HUB_ADMINISTRATOR || user.Role == Role.SYSTEM;
        bool isOrganizationManager = user.Role == Role.ORGANIZATION_MANAGER && user.OrganizationId == Tag.OrganizationId;
        if (!isSystemAdmin && !isOrganizationManager)
            throw new AppException(StatusCodes.Status403Forbidden, "User does not have permission to update this Tag.");

        //Apply updates to the Tag
        if (request.ExcludedFromAi.HasValue)
            Tag.ExcludedFromAi = request.ExcludedFromAi.Value;

        //Save changes to the database
        await _db.SaveChangesAsync(cancellationToken);

        //Publish an event to notify other services of the Tag update
        TagAISettingUpdatedEvent tagAISettingUpdatedEvent = new(Tag.Id, Tag.OrganizationId, request.UserId, Tag.ExcludedFromAi);
        await _publisher.PublishAsync("TagAISettingUpdated", tagAISettingUpdatedEvent, cancellationToken);

        return true;
    }
}