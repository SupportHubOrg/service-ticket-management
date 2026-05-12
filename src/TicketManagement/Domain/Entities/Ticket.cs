using System.ComponentModel.DataAnnotations;
using SupportHub.TicketManagement.Domain.Enums;

namespace SupportHub.TicketManagement.Domain.Entities
{
    public class Ticket
    {
        [Key]
        public Guid Id { get; set; } = default!;
        
        [Required]
        public Guid CreatorId { get; set; } = default!;

        public Guid? AssignedOperatorId { get; set; }

        [Required]
        [StringLength(200, ErrorMessage = "Subject cannot be longer than 200 characters!")]
        [MinLength(5, ErrorMessage = "Subject must be at least 5 characters long!")]
        public string Subject { get; set; } = default!;
        
        [Required]
        [StringLength(10000, ErrorMessage = "Description cannot be longer than 10000 characters!")]
        [MinLength(15, ErrorMessage = "Description must be at least 15 characters long!")]
        public string Description { get; set; } = default!;

        [Required]
        [EnumDataType(typeof(Status))]
        public Status Status { get; set; } = default!;

        public DateTime? SlaFirstResponseDeadline { get; set; }
        public DateTime? SlaHumanResponseDeadline { get; set; }
        public DateTime? SlaResolutionDeadline { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public UserSnapshot Creator { get; set; } = null!;
        public UserSnapshot AssignedOperator { get; set; } = null!;

        public ICollection<TicketTag> TicketTags { get; set; } = [];
        public ICollection<TicketMessage> Messages { get; set; } = [];
        public ICollection<TicketAuditLog> AuditLogs { get; set; } = [];
    }
}