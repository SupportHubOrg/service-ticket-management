namespace SupportHub.TicketManagement.Domain.Enums
{
    public enum Role
    {
        AI = 0,
        EMPLOYEE = 1,
        LEVEL_1_OPERATOR = 2,
        LEVEL_2_OPERATOR = 3,
        ORGANIZATION_MANAGER = 4,
        SUPPORT_HUB_ADMINISTRATOR = 5,

        SYSTEM = 99 //Special role used for system-initiated actions/events where no real user is involved  
    }
}
