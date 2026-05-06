using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ProductApi.Models;

namespace TiendaPromElec.Services
{
    /// <summary>
    /// Generación de tokens JWT firmados con HMAC-SHA256.
    /// El secreto NO se guarda en appsettings.json (mitiga OWASP A02 - Cryptographic Failures);
    /// se lee desde User Secrets en desarrollo y variables de entorno en producción.
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;

        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        public (string Token, DateTime ExpiresAt) GenerateToken(ApplicationUser user, IList<string> roles)
        {
            var jwtSecret = _config["Jwt:Secret"]
                ?? throw new InvalidOperationException("Jwt:Secret no configurado. Usa User Secrets o variables de entorno.");
            var issuer = _config["Jwt:Issuer"] ?? "TiendaPromElec";
            var audience = _config["Jwt:Audience"] ?? "TiendaPromElecClients";
            var expiresMinutes = int.TryParse(_config["Jwt:ExpiresMinutes"], out var m) ? m : 60;

            if (jwtSecret.Length < 32)
                throw new InvalidOperationException("Jwt:Secret debe tener al menos 32 caracteres.");

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiresAt = DateTime.UtcNow.AddMinutes(expiresMinutes);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }
    }
}
