namespace SupportHub.TicketManagement.Domain.Enums
{
    public enum Status
    {
        NEW = 0,
        IN_PROGRESS_LEVEL_1 = 1,
        IN_PROGRESS_LEVEL_2 = 2,
        UNASSIGNED = 3,
        AWAITING_CUSTOMER_RESPONSE = 4,
        CLOSED = 5,
        IN_KNOWLEDGE_BASE = 6,
        NOT_IN_KNOWLEDGE_BASE = 7,
        SOLUTION_IMPOSSIBLE = 8
    }
}