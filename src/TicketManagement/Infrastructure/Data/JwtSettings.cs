using System.ComponentModel.DataAnnotations;

namespace SupportHub.TicketManagement.Infrastructure.Data
{
    public class JwtSettings
    {
        [Required]
        public string Secret { get; set; } = default!;

        [Required]
        public string Issuer { get; set; } = default!;

        [Required]
        public string Audience { get; set; } = default!;

        public int ExpiryMinutes { get; set; } = 3600;
    }
}
