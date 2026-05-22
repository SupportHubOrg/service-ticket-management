using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.Tags.GetTag;

public class GetTagHandler(TicketDbContext db) : IRequestHandler<GetTagQuery, TagDetailsResponse>
{
    private readonly TicketDbContext _db = db;

    public async Task<TagDetailsResponse> Handle(GetTagQuery request, CancellationToken cancellationToken)
    {
        //Retrieve the Tag with AsNoTracking for read-optimization
        Tag? Tag = await _db.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TagId, cancellationToken);
        //Check if the Tag exists and return 404 Not Found if it doesn't
        if (Tag == null)
            throw new AppException(StatusCodes.Status404NotFound, $"Tag {request.TagId} could not be found.");

        //Retrieve the user requesting the Tag
        UserSnapshot? user = await _db.UserSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        //Check if the user exists and return 401 Unauthorized if not
        if (user == null)
            throw new AppException(StatusCodes.Status401Unauthorized, "User could not be found.");

        //Enforce Data Visibility Rules
        bool isSystemAdmin = user.Role == Role.SUPPORT_HUB_ADMINISTRATOR || user.Role == Role.SYSTEM;
        bool isSameOrganization = user.OrganizationId == Tag.OrganizationId;
        //Check if the Tag belongs to the same organization as the user, otherwise return 403 Forbidden
        if (!isSystemAdmin && !isSameOrganization)
            throw new AppException(StatusCodes.Status403Forbidden, "You do not have permission to view this Tag.");

        //Map the Database Entity to the DTO and return it
        return new TagDetailsResponse(
            Tag.Id,
            Tag.OrganizationId,
            Tag.Name,
            Tag.Description,
            Tag.ExcludedFromAi
        );
    }
}