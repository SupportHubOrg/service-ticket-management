using System.ComponentModel.DataAnnotations;
using SupportHub.TicketManagement.Domain.Enums;

namespace SupportHub.TicketManagement.Domain.Entities
{
    public class TicketAuditLog
    {
        [Key]
        public Guid Id { get; set; } = default!;

        [Required]
        public Guid TicketId { get; set; } = default!;
        
        public Guid? ChangedById { get; set; }

        [Required]
        [EnumDataType(typeof(Role), ErrorMessage = "ActorType must be a valid Role enum value!")]
        public Role ActorType { get; set; } = default!;

        [Required]
        [StringLength(255, ErrorMessage = "Action cannot be longer than 255 characters!")]
        public string Action { get; set; } = default!;

        [StringLength(255, ErrorMessage = "OldValue cannot be longer than 255 characters!")]
        public string? OldValue { get; set; } = default!;

        [StringLength(255, ErrorMessage = "NewValue cannot be longer than 255 characters!")]
        public string? NewValue { get; set; } = default!;

        [Required]
        [StringLength(2000, ErrorMessage = "Message cannot be longer than 2000 characters!")]
        [MinLength(1, ErrorMessage = "Message must be at least 1 character long!")]
        public string Message { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        public Ticket Ticket { get; set; } = null!;
        public UserSnapshot ChangedBy { get; set; } = null!;
    }
}