using System.ComponentModel.DataAnnotations;

namespace TiendaPromElec.DTOs
{
    public class RegisterDto
    {
        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        // Mínimo 8 caracteres + complejidad para mitigar OWASP A07
        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
