using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ProductApi.Models;
using TiendaPromElec.DTOs;
using TiendaPromElec.IntegrationTests.Helpers;

namespace TiendaPromElec.IntegrationTests.Security
{
    /// <summary>
    /// Pruebas de SEGURIDAD: 5 positivas + 5 negativas.
    /// Verifican el correcto funcionamiento de la autenticación JWT,
    /// las políticas de autorización, las cabeceras de seguridad y CORS.
    /// </summary>
    public class SecurityTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public SecurityTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // =========================================================
        //                      POSITIVAS
        // =========================================================

        // =================== POSITIVA #1: Admin puede crear ===================
        [Fact]
        public async Task AdminToken_CanPostProduct_Returns201()
        {
            var client = _factory.CreateClient();
            var token = await AuthHelper.LoginAsAdminAsync(client);
            AuthHelper.SetBearer(client, token);

            var resp = await client.PostAsJsonAsync("/api/Product", new ProductCreateDto
            {
                Name = "Sec Pos 1",
                Description = "ok",
                Brand = "x",
                Price = 1.50m,
                Stock = 1,
                CategoryId = 1
            });

            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        }

        // =================== POSITIVA #2: Admin puede eliminar ===================
        [Fact]
        public async Task AdminToken_CanDeleteProduct_Returns204()
        {
            var client = _factory.CreateClient();
            var token = await AuthHelper.LoginAsAdminAsync(client);
            AuthHelper.SetBearer(client, token);

            // Primero crea uno
            var createResp = await client.PostAsJsonAsync("/api/Product", new ProductCreateDto
            {
                Name = "ToDelete",
                Description = "x",
                Brand = "x",
                Price = 1m,
                Stock = 1,
                CategoryId = 1
            });
            var prod = await createResp.Content.ReadFromJsonAsync<Product>();

            var delResp = await client.DeleteAsync($"/api/Product/{prod!.Id}");
            Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);
        }

        // =================== POSITIVA #3: Usuario autenticado puede crear pedido ===================
        [Fact]
        public async Task AuthenticatedUser_CanCreateOrder_Returns201()
        {
            var client = _factory.CreateClient();
            var token = await AuthHelper.RegisterAndLoginUserAsync(client, "secpos3");
            AuthHelper.SetBearer(client, token);

            var resp = await client.PostAsJsonAsync("/api/Order", new
            {
                orderDate = DateTime.UtcNow,
                totalAmount = 100m,
                status = "Pendiente",
                customerId = 1
            });
            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        }

        // =================== POSITIVA #4: Cabeceras de seguridad presentes ===================
        [Fact]
        public async Task Response_IncludesSecurityHeaders()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/Product");

            Assert.True(resp.Headers.Contains("X-Content-Type-Options"));
            Assert.True(resp.Headers.Contains("X-Frame-Options"));
            Assert.Equal("nosniff", string.Join(",", resp.Headers.GetValues("X-Content-Type-Options")));
            Assert.Equal("DENY", string.Join(",", resp.Headers.GetValues("X-Frame-Options")));
        }

        // =================== POSITIVA #5: Token de admin permite ver lista de pedidos ===================
        [Fact]
        public async Task AdminToken_CanListOrders_Returns200()
        {
            var client = _factory.CreateClient();
            var token = await AuthHelper.LoginAsAdminAsync(client);
            AuthHelper.SetBearer(client, token);

            var resp = await client.GetAsync("/api/Order");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        // =========================================================
        //                      NEGATIVAS
        // =========================================================

        // =================== NEGATIVA #1: Sin token al crear producto ===================
        [Fact]
        public async Task NoToken_PostProduct_Returns401()
        {
            var client = _factory.CreateClient();
            var resp = await client.PostAsJsonAsync("/api/Product", new ProductCreateDto
            {
                Name = "Neg1",
                Description = "x",
                Brand = "x",
                Price = 1m,
                Stock = 1,
                CategoryId = 1
            });
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        // =================== NEGATIVA #2: Usuario normal NO puede crear (403) ===================
        [Fact]
        public async Task NormalUserToken_PostProduct_Returns403()
        {
            var client = _factory.CreateClient();
            var token = await AuthHelper.RegisterAndLoginUserAsync(client, "normaluser");
            AuthHelper.SetBearer(client, token);

            var resp = await client.PostAsJsonAsync("/api/Product", new ProductCreateDto
            {
                Name = "Neg2",
                Description = "x",
                Brand = "x",
                Price = 1m,
                Stock = 1,
                CategoryId = 1
            });
            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        // =================== NEGATIVA #3: Token mal formado ===================
        [Fact]
        public async Task MalformedToken_PostProduct_Returns401()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "esto-no-es-un-jwt");

            var resp = await client.PostAsJsonAsync("/api/Product", new ProductCreateDto
            {
                Name = "Neg3",
                Description = "x",
                Brand = "x",
                Price = 1m,
                Stock = 1,
                CategoryId = 1
            });
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        // =================== NEGATIVA #4: Sin token al borrar producto ===================
        [Fact]
        public async Task NoToken_DeleteProduct_Returns401()
        {
            var client = _factory.CreateClient();
            var resp = await client.DeleteAsync("/api/Product/1");
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        // =================== NEGATIVA #5: Usuario normal NO puede listar pedidos (admin only) ===================
        [Fact]
        public async Task NormalUserToken_ListOrders_Returns403()
        {
            var client = _factory.CreateClient();
            var token = await AuthHelper.RegisterAndLoginUserAsync(client, "normallist");
            AuthHelper.SetBearer(client, token);

            var resp = await client.GetAsync("/api/Order");
            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }
    }
}
