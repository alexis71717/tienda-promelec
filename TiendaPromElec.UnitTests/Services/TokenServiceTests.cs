using Microsoft.Extensions.Configuration;
using ProductApi.Models;
using TiendaPromElec.Services;

namespace TiendaPromElec.UnitTests.Services
{
    public class TokenServiceTests
    {
        private static IConfiguration BuildConfig(string? secret)
        {
            var dict = new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = secret,
                ["Jwt:Issuer"] = "TiendaPromElec",
                ["Jwt:Audience"] = "TiendaPromElecClients",
                ["Jwt:ExpiresMinutes"] = "60"
            };
            return new ConfigurationBuilder().AddInMemoryCollection(dict!).Build();
        }

        // ===================== NEGATIVA #4 =====================
        [Fact]
        public void GenerateToken_NoSecretConfigured_ThrowsInvalidOperation()
        {
            var config = BuildConfig(secret: null);
            var service = new TokenService(config);
            var user = new ApplicationUser { Id = "1", Email = "x@x.com", UserName = "x@x.com" };

            Assert.Throws<InvalidOperationException>(() =>
                service.GenerateToken(user, new[] { "User" }));
        }
    }
}
