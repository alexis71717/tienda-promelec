using ProductApi.Models;

namespace TiendaPromElec.Services
{
    public interface ITokenService
    {
        /// <summary>
        /// Genera un JWT firmado para el usuario y sus roles.
        /// </summary>
        (string Token, DateTime ExpiresAt) GenerateToken(ApplicationUser user, IList<string> roles);
    }
}
