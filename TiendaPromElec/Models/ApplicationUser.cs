using Microsoft.AspNetCore.Identity;

namespace ProductApi.Models
{
    /// <summary>
    /// Usuario de la aplicación. Extiende IdentityUser para integrarse con
    /// ASP.NET Core Identity y aprovechar el hashing de contraseñas, manejo
    /// de roles y soporte para 2FA, lockout, etc. (mitiga A02 y A07 OWASP).
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
    }
}
