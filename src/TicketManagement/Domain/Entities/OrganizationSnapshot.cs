using System.ComponentModel.DataAnnotations;

namespace SupportHub.TicketManagement.Domain.Entities
{
    /// <summary>
    /// Represents a snapshot of an organization's settings and configurations at the time of a user's interaction.
    /// The original table is stored in the organization access service, but a local copy is maintained to avoid cross-service calls during ticket operations.
    /// </summary>
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