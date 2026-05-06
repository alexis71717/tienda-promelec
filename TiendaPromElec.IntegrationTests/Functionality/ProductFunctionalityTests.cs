using System.Net;
using System.Net.Http.Json;
using ProductApi.Models;
using TiendaPromElec.DTOs;
using TiendaPromElec.IntegrationTests.Helpers;

namespace TiendaPromElec.IntegrationTests.Functionality
{
    /// <summary>
    /// Pruebas de FUNCIONALIDAD: 5 positivas + 5 negativas.
    /// </summary>
    public class ProductFunctionalityTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ProductFunctionalityTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // =================== POSITIVA #1: Registro válido ===================
        [Fact]
        public async Task Register_ValidPayload_Returns200()
        {
            var client = _factory.CreateClient();
            var resp = await client.PostAsJsonAsync("/api/auth/register", new RegisterDto
            {
                FullName = "Func User",
                Email = $"func_{Guid.NewGuid():N}@test.com",
                Password = "User#2025!"
            });
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var body = await resp.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(body);
            Assert.False(string.IsNullOrEmpty(body!.Token));
        }

        // =================== POSITIVA #2: Login válido ===================
        [Fact]
        public async Task Login_ValidCredentials_Returns200WithToken()
        {
            var client = _factory.CreateClient();
            var resp = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
            {
                Email = "admin@promelec.com",
                Password = "Admin#2025!"
            });
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var body = await resp.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(body);
            Assert.Contains("Admin", body!.Roles);
        }

        // =================== POSITIVA #3: Listado público de productos ===================
        [Fact]
        public async Task GetProducts_Anonymous_Returns200WithList()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/Product");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var products = await resp.Content.ReadFromJsonAsync<List<Product>>();
            Assert.NotNull(products);
        }

        // =================== POSITIVA #4: GET por id existente (seed) ===================
        [Fact]
        public async Task GetProductById_ExistingId_Returns200()
        {
            var client = _factory.CreateClient();
            // Primero creamos uno (con admin) para garantizar que exista
            var token = await AuthHelper.LoginAsAdminAsync(client);
            AuthHelper.SetBearer(client, token);

            var createResp = await client.PostAsJsonAsync("/api/Product", new ProductCreateDto
            {
                Name = "FuncTest",
                Description = "Test",
                Brand = "B",
                Price = 100m,
                Stock = 5,
                CategoryId = 1
            });
            createResp.EnsureSuccessStatusCode();
            var created = await createResp.Content.ReadFromJsonAsync<Product>();

            var getResp = await client.GetAsync($"/api/Product/{created!.Id}");
            Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        }

        // =================== POSITIVA #5: Crear producto (admin) ===================
        [Fact]
        public async Task CreateProduct_AsAdmin_Returns201()
        {
            var client = _factory.CreateClient();
            var token = await AuthHelper.LoginAsAdminAsync(client);
            AuthHelper.SetBearer(client, token);

            var resp = await client.PostAsJsonAsync("/api/Product", new ProductCreateDto
            {
                Name = "Producto Nuevo",
                Description = "Descripción válida",
                Brand = "Marca",
                Price = 199.99m,
                Stock = 20,
                CategoryId = 1
            });

            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
            var product = await resp.Content.ReadFromJsonAsync<Product>();
            Assert.NotNull(product);
            Assert.True(product!.Id > 0);
        }

        // =================== NEGATIVA #1: Registro con email duplicado ===================
        [Fact]
        public async Task Register_DuplicateEmail_Returns400()
        {
            var client = _factory.CreateClient();
            var email = $"dup_{Guid.NewGuid():N}@test.com";
            var dto = new RegisterDto { FullName = "Dup", Email = email, Password = "User#2025!" };

            var first = await client.PostAsJsonAsync("/api/auth/register", dto);
            first.EnsureSuccessStatusCode();

            var second = await client.PostAsJsonAsync("/api/auth/register", dto);
            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        }

        // =================== NEGATIVA #2: Login con password incorrecto ===================
        [Fact]
        public async Task Login_WrongPassword_Returns401()
        {
            var client = _factory.CreateClient();
            var resp = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
            {
                Email = "admin@promelec.com",
                Password = "PasswordIncorrecto#1"
            });
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        // =================== NEGATIVA #3: GET de id inexistente ===================
        [Fact]
        public async Task GetProductById_NonExisting_Returns404()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/Product/99999");
            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        // =================== NEGATIVA #4: Crear producto con datos inválidos ===================
        [Fact]
        public async Task CreateProduct_InvalidModel_Returns400()
        {
            var client = _factory.CreateClient();
            var token = await AuthHelper.LoginAsAdminAsync(client);
            AuthHelper.SetBearer(client, token);

            // Price negativo y Stock negativo, Name vacío -> debe fallar validación
            var resp = await client.PostAsJsonAsync("/api/Product", new ProductCreateDto
            {
                Name = "",
                Description = "",
                Brand = "",
                Price = -10,
                Stock = -1,
                CategoryId = 1
            });
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        // =================== NEGATIVA #5: DELETE de producto inexistente ===================
        [Fact]
        public async Task DeleteProduct_NonExisting_Returns404()
        {
            var client = _factory.CreateClient();
            var token = await AuthHelper.LoginAsAdminAsync(client);
            AuthHelper.SetBearer(client, token);

            var resp = await client.DeleteAsync("/api/Product/99999");
            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }
    }
}
