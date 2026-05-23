using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Messaging;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.Tags.CreateTag;

/// <summary>
/// Handles the creation of a new support Tag.
/// </summary>
/// <param name="db"></param>
/// <param name="publisher"></param>
/// <param name="logger"></param>
public class CreateTagHandler(TicketDbContext db, IEventPublisher publisher) : IRequestHandler<CreateTagCommand, Guid>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;

    public async Task<Guid> Handle(CreateTagCommand request, CancellationToken cancellationToken)
    {
        //Retrieve the user and their organization and check if the user exists, otherwise return 401 Unauthorized
        UserSnapshot? user = await _db.UserSnapshots
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
            throw new AppException(StatusCodes.Status401Unauthorized, "User could not be found.");

        //Check if the organization exists and return 404 Not Found if not
        if (_db.OrganizationSnapshots.Any(os => os.Id == request.OrganizationId))
            throw new AppException(StatusCodes.Status404NotFound, "Organization could not be found.");

        //Check if the user has permissions to manage tags in the organization
        bool isSystemAdmin = user.Role == Role.SUPPORT_HUB_ADMINISTRATOR || user.Role == Role.SYSTEM;
        bool isOrganizationManager = user.Role == Role.ORGANIZATION_MANAGER && user.OrganizationId == request.OrganizationId;
        if (!isSystemAdmin && !isOrganizationManager)
            throw new AppException(StatusCodes.Status403Forbidden, "User does not have permissions to manage tags in the organization.");

        //Check if the name of the tag is unique within the organization
        bool nameExists = await _db.Tags.AnyAsync(t => t.OrganizationId == user.OrganizationId && t.Name == request.Name, cancellationToken);
        if (nameExists)
            throw new AppException(StatusCodes.Status400BadRequest, "A tag with the same name already exists in the organization.");

        //Create the new Tag
        var Tag = new Tag
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            Name = request.Name,
            Description = request.Description,
            ExcludedFromAi = request.ExcludedFromAi
        };

        //Save the new Tag to the database
        _db.Tags.Add(Tag);
        await _db.SaveChangesAsync(cancellationToken);

        //Return the assigned ID
        return Tag.Id;
    }
}