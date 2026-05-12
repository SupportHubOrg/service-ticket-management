using System.ComponentModel.DataAnnotations;
using SupportHub.TicketManagement.Domain.Enums;

namespace SupportHub.TicketManagement.Domain.Entities
{
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
    }
}