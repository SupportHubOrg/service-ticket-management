using System.ComponentModel.DataAnnotations;

namespace SupportHub.TicketManagement.Domain.Entities
{
    public class OperatorTag
    {
        [Required]
        public Guid OperatorId { get; set; } = default!;
        
        [Required]
        public Guid TagId { get; set; } = default!;

        public Tag Tag { get; set; } = null!;
        public UserSnapshot Operator { get; set; } = null!;
    }
}