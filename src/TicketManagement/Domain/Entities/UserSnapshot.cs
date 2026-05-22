using System.ComponentModel.DataAnnotations;
using SupportHub.TicketManagement.Domain.Enums;

namespace SupportHub.TicketManagement.Domain.Entities
{
    /// <summary>
    /// Represents a snapshot of a user at the time of ticket creation or modification.
    /// The original table is stored in the organization access service, but a local copy is maintained to avoid cross-service calls during ticket operations.
    /// </summary>
    public class UserSnapshot
    {
        [Key]
        public Guid Id { get; set; } = default!;

        [Required]
        public Guid OrganizationId { get; set; } = default!;
        
        [Required]
        [EnumDataType(typeof(Role))]
        public Role Role { get; set; } = default!;

        public OrganizationSnapshot Organization { get; set; } = null!;

        public ICollection<TicketAuditLog> TicketAuditLogs { get; set; } = [];
        public ICollection<Ticket> CreatedTickets { get; set; } = [];
        public ICollection<Ticket> AssignedTickets { get; set; } = [];
        public ICollection<TicketTag> AssignedTicketTags { get; set; } = [];
    }
}