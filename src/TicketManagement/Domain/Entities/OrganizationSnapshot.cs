using System.ComponentModel.DataAnnotations;

namespace SupportHub.TicketManagement.Domain.Entities
{
    public class OrganizationSnapshot
    {
        [Key]
        public Guid Id { get; set; } = default!;
        
        [Required]
        public decimal SlaFirstResponseMinutes { get; set; } = default!;

        [Required]
        public decimal AiTrustThreshold { get; set; } = default!;

        [Required]
        public decimal SlaResolutionMinutes { get; set; } = default!;

        [Required]
        public decimal SlaHumanResponseMinutes { get; set; } = default!;
        

        public ICollection<UserSnapshot> Users { get; set; } = [];
    }
}