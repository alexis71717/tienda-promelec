using TiendaPromElec.DTOs;

namespace TiendaPromElec.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string? ErrorMessage, AuthResponseDto? Response)> RegisterAsync(RegisterDto dto);
        Task<(bool Success, string? ErrorMessage, AuthResponseDto? Response)> LoginAsync(LoginDto dto);
    }
}
