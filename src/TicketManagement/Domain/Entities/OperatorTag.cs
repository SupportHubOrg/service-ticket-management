using System.ComponentModel.DataAnnotations;

namespace SupportHub.TicketManagement.Domain.Entities
{
    /// <summary>
    /// Represents the many-to-many relationship between support ticket operators and tags.
    /// Operators are assigned tags based on their expertise and skills, which can be used for intelligent ticket routing and assignment.
    /// </summary>
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