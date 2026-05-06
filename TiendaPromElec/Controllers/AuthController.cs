using Microsoft.AspNetCore.Mvc;
using TiendaPromElec.DTOs;
using TiendaPromElec.Services;

namespace TiendaPromElec.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Registra un nuevo usuario con rol "User".
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (success, error, response) = await _authService.RegisterAsync(dto);
            if (!success) return BadRequest(new { message = error });
            return Ok(response);
        }

        /// <summary>
        /// Autentica un usuario y devuelve un JWT.
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (success, error, response) = await _authService.LoginAsync(dto);
            if (!success) return Unauthorized(new { message = error });
            return Ok(response);
        }
    }
}
