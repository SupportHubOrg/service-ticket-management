using System.ComponentModel.DataAnnotations;

namespace SupportHub.TicketManagement.Domain.Entities
{
    public class Tag
    {
        [Key]
        public Guid Id { get; set; } = default!;

        [Required]
        public Guid OrganizationId { get; set; } = default!;

        [Required]
        [StringLength(200, ErrorMessage = "Name cannot be longer than 200 characters!")]
        [MinLength(5, ErrorMessage = "Name must be at least 5 characters long!")]
        public string Name { get; set; } = default!;

        [Required]
        [StringLength(10000, ErrorMessage = "Description cannot be longer than 10000 characters!")]
        [MinLength(15, ErrorMessage = "Description must be at least 15 characters long!")]
        public string Description { get; set; } = default!;

        [Required]
        public bool ExcludedFromAi { get; set; } = false;
        
        public ICollection<TicketTag> TicketTags { get; set; } = [];
        public ICollection<OperatorTag> OperatorTags { get; set; } = [];
    }
}