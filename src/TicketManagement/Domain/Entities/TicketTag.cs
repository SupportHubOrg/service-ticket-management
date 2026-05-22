using System.ComponentModel.DataAnnotations;
using SupportHub.TicketManagement.Domain.Enums;

namespace SupportHub.TicketManagement.Domain.Entities
{
    public class TicketTag
    {
        [Required]
        public Guid TicketId { get; set; } = default!;
        
        [Required]
        public Guid TagId { get; set; } = default!;
        
        [Required]
        public Guid AssignedById { get; set; } = default!;

        public Ticket Ticket { get; set; } = null!;
        public Tag Tag { get; set; } = null!;
        public UserSnapshot AssignedBy { get; set; } = null!;
    }
}