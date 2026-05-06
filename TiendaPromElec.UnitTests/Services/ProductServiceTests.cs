using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProductApi.Models;
using TiendaPromElec.DTOs;
using TiendaPromElec.Services;

namespace TiendaPromElec.UnitTests.Services
{
    /// <summary>
    /// Pruebas unitarias del servicio de productos.
    /// Usa EF Core InMemory + Moq para ILogger (uso correcto de mocks).
    /// </summary>
    public class ProductServiceTests
    {
        private static AppDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new AppDbContext(options);
        }

        // ===================== POSITIVA #1 =====================
        [Fact]
        public async Task GetAllAsync_WithSeedData_ReturnsAllProducts()
        {
            using var ctx = CreateInMemoryContext(nameof(GetAllAsync_WithSeedData_ReturnsAllProducts));
            ctx.Products.AddRange(
                new Product { Id = 1, Name = "P1", Description = "D1", Brand = "B1", Price = 100, Stock = 5, CategoryId = 1 },
                new Product { Id = 2, Name = "P2", Description = "D2", Brand = "B2", Price = 200, Stock = 3, CategoryId = 1 }
            );
            await ctx.SaveChangesAsync();

            var loggerMock = new Mock<ILogger<ProductService>>();
            var service = new ProductService(ctx, loggerMock.Object);

            var result = await service.GetAllAsync();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        // ===================== POSITIVA #2 =====================
        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsProduct_AndLogs()
        {
            using var ctx = CreateInMemoryContext(nameof(GetByIdAsync_ExistingId_ReturnsProduct_AndLogs));
            ctx.Products.Add(new Product { Id = 42, Name = "Galaxy S25", Description = "Smartphone", Brand = "Samsung", Price = 24999, Stock = 5, CategoryId = 1 });
            await ctx.SaveChangesAsync();

            var loggerMock = new Mock<ILogger<ProductService>>();
            var service = new ProductService(ctx, loggerMock.Object);

            var result = await service.GetByIdAsync(42);

            Assert.NotNull(result);
            Assert.Equal("Galaxy S25", result!.Name);

            // Verificación del mock: confirma que el logger fue invocado
            loggerMock.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
        }

        // ===================== POSITIVA #3 =====================
        [Fact]
        public async Task CreateAsync_ValidDto_PersistsAndReturnsProduct()
        {
            using var ctx = CreateInMemoryContext(nameof(CreateAsync_ValidDto_PersistsAndReturnsProduct));
            var loggerMock = new Mock<ILogger<ProductService>>();
            var service = new ProductService(ctx, loggerMock.Object);

            var dto = new ProductCreateDto
            {
                Name = "Nuevo",
                Description = "Desc",
                Brand = "Marca",
                Price = 99.9m,
                Stock = 10,
                CategoryId = 1
            };

            var created = await service.CreateAsync(dto);

            Assert.NotNull(created);
            Assert.True(created.Id > 0);
            Assert.Equal("Nuevo", created.Name);
            Assert.Equal(1, await ctx.Products.CountAsync());
        }

        // ===================== POSITIVA #4 =====================
        [Fact]
        public async Task DeleteAsync_ExistingId_RemovesAndReturnsTrue()
        {
            using var ctx = CreateInMemoryContext(nameof(DeleteAsync_ExistingId_RemovesAndReturnsTrue));
            ctx.Products.Add(new Product { Id = 7, Name = "Delete me", Description = "D", Brand = "B", Price = 1, Stock = 1, CategoryId = 1 });
            await ctx.SaveChangesAsync();

            var loggerMock = new Mock<ILogger<ProductService>>();
            var service = new ProductService(ctx, loggerMock.Object);

            var ok = await service.DeleteAsync(7);

            Assert.True(ok);
            Assert.Null(await ctx.Products.FindAsync(7L));
        }

        // ===================== NEGATIVA #1 =====================
        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            using var ctx = CreateInMemoryContext(nameof(GetByIdAsync_NonExistingId_ReturnsNull));
            var loggerMock = new Mock<ILogger<ProductService>>();
            var service = new ProductService(ctx, loggerMock.Object);

            var result = await service.GetByIdAsync(99999);

            Assert.Null(result);
        }

        // ===================== NEGATIVA #2 =====================
        [Fact]
        public async Task UpdateAsync_NonExistingId_ReturnsFalse()
        {
            using var ctx = CreateInMemoryContext(nameof(UpdateAsync_NonExistingId_ReturnsFalse));
            var loggerMock = new Mock<ILogger<ProductService>>();
            var service = new ProductService(ctx, loggerMock.Object);

            var ok = await service.UpdateAsync(99999, new ProductUpdateDto
            {
                Name = "X",
                Description = "X",
                Brand = "X",
                Price = 1,
                Stock = 1,
                CategoryId = 1
            });

            Assert.False(ok);
        }

        // ===================== NEGATIVA #3 =====================
        [Fact]
        public async Task DeleteAsync_NonExistingId_ReturnsFalse()
        {
            using var ctx = CreateInMemoryContext(nameof(DeleteAsync_NonExistingId_ReturnsFalse));
            var loggerMock = new Mock<ILogger<ProductService>>();
            var service = new ProductService(ctx, loggerMock.Object);

            var ok = await service.DeleteAsync(99999);

            Assert.False(ok);
        }
    }
}
