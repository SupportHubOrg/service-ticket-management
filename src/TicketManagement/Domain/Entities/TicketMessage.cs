using System.ComponentModel.DataAnnotations;
using SupportHub.TicketManagement.Domain.Enums;

namespace SupportHub.TicketManagement.Domain.Entities
{
    /// <summary>
    /// Represents a message or comment added to a support ticket, which can be created by either the ticket creator, an operator, or an automated system.
    /// Each message is associated with a specific ticket and includes information about the sender and the content of the message.
    /// </summary>
    public class TicketMessage
    {
        [Key]
        public Guid Id { get; set; } = default!;

        [Required]
        public Guid TicketId { get; set; } = default!;
        
        public Guid SenderId { get; set; }
        
        [Required]
        public Role SenderType { get; set; } = default!;

        [Required]
        [StringLength(2000, ErrorMessage = "Message cannot be longer than 2000 characters!")]
        [MinLength(1, ErrorMessage = "Message must be at least 1 character long!")]
        public string Message { get; set; } = default!;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;


        public Ticket Ticket { get; set; } = null!;
        public UserSnapshot Sender { get; set; } = null!;
    }
}