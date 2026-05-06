using System.Net.Http.Headers;
using System.Net.Http.Json;
using TiendaPromElec.DTOs;

namespace TiendaPromElec.IntegrationTests.Helpers
{
    /// <summary>
    /// Helper para registrar/loguear usuarios en pruebas de integración
    /// y agregar el token JWT a HttpClient.
    /// </summary>
    public static class AuthHelper
    {
        public static async Task<string> LoginAsAdminAsync(HttpClient client)
        {
            var resp = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
            {
                Email = "admin@promelec.com",
                Password = "Admin#2025!"
            });
            resp.EnsureSuccessStatusCode();
            var dto = await resp.Content.ReadFromJsonAsync<AuthResponseDto>();
            return dto!.Token;
        }

        public static async Task<string> RegisterAndLoginUserAsync(HttpClient client, string emailPrefix)
        {
            var email = $"{emailPrefix}_{Guid.NewGuid():N}@test.com";
            var registerResp = await client.PostAsJsonAsync("/api/auth/register", new RegisterDto
            {
                FullName = "Test User",
                Email = email,
                Password = "User#2025!"
            });
            registerResp.EnsureSuccessStatusCode();
            var dto = await registerResp.Content.ReadFromJsonAsync<AuthResponseDto>();
            return dto!.Token;
        }

        public static void SetBearer(HttpClient client, string token)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
