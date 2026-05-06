using Microsoft.AspNetCore.Mvc;
using Moq;
using TiendaPromElec.Controllers;
using TiendaPromElec.DTOs;
using TiendaPromElec.Services;

namespace TiendaPromElec.UnitTests.Controllers
{
    public class AuthControllerTests
    {
        // ============ NEGATIVA en controller (login fallido) ============
        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange: mock del IAuthService devolviendo (false, error, null)
            var mockAuth = new Mock<IAuthService>();
            mockAuth.Setup(s => s.LoginAsync(It.IsAny<LoginDto>()))
                    .ReturnsAsync((false, "Credenciales inválidas.", (AuthResponseDto?)null));

            var controller = new AuthController(mockAuth.Object);

            // Act
            var result = await controller.Login(new LoginDto { Email = "no@no.com", Password = "Bad#1234" });

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorized.StatusCode);
            mockAuth.Verify(s => s.LoginAsync(It.IsAny<LoginDto>()), Times.Once);
        }

        // ============ POSITIVA en controller (login OK) ============
        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkWithToken()
        {
            var mockAuth = new Mock<IAuthService>();
            var responseDto = new AuthResponseDto
            {
                Token = "fake.jwt.token",
                Email = "ok@ok.com",
                FullName = "OK User",
                Roles = new List<string> { "User" },
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
            mockAuth.Setup(s => s.LoginAsync(It.IsAny<LoginDto>()))
                    .ReturnsAsync((true, null, responseDto));

            var controller = new AuthController(mockAuth.Object);

            var result = await controller.Login(new LoginDto { Email = "ok@ok.com", Password = "Good#1234" });

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<AuthResponseDto>(ok.Value);
            Assert.Equal("fake.jwt.token", body.Token);
            Assert.Contains("User", body.Roles);
        }
    }
}
