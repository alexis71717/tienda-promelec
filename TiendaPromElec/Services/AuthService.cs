using Microsoft.AspNetCore.Identity;
using ProductApi.Models;
using TiendaPromElec.DTOs;

namespace TiendaPromElec.Services
{
    /// <summary>
    /// Servicio de autenticación: usa ASP.NET Core Identity para hash de
    /// contraseñas (PBKDF2) y políticas de password fuertes.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        public const string RoleAdmin = "Admin";
        public const string RoleUser = "User";

        public AuthService(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<(bool Success, string? ErrorMessage, AuthResponseDto? Response)> RegisterAsync(RegisterDto dto)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
            {
                _logger.LogWarning("Intento de registro con email ya existente: {Email}", dto.Email);
                return (false, "El email ya está registrado.", null);
            }

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Registro fallido para {Email}: {Errors}", dto.Email, errors);
                return (false, errors, null);
            }

            // Por defecto los usuarios nuevos reciben el rol "User"
            await _userManager.AddToRoleAsync(user, RoleUser);

            var roles = await _userManager.GetRolesAsync(user);
            var (token, expiresAt) = _tokenService.GenerateToken(user, roles);

            _logger.LogInformation("Usuario registrado: {Email}", dto.Email);
            return (true, null, new AuthResponseDto
            {
                Token = token,
                Email = user.Email!,
                FullName = user.FullName ?? string.Empty,
                Roles = roles,
                ExpiresAt = expiresAt
            });
        }

        public async Task<(bool Success, string? ErrorMessage, AuthResponseDto? Response)> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                // Mensaje genérico para no filtrar si el email existe (OWASP A07)
                _logger.LogWarning("Login fallido (usuario no existe): {Email}", dto.Email);
                return (false, "Credenciales inválidas.", null);
            }

            var validPassword = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!validPassword)
            {
                _logger.LogWarning("Login fallido (password incorrecto): {Email}", dto.Email);
                return (false, "Credenciales inválidas.", null);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var (token, expiresAt) = _tokenService.GenerateToken(user, roles);

            _logger.LogInformation("Login exitoso: {Email}", dto.Email);
            return (true, null, new AuthResponseDto
            {
                Token = token,
                Email = user.Email!,
                FullName = user.FullName ?? string.Empty,
                Roles = roles,
                ExpiresAt = expiresAt
            });
        }
    }
}
