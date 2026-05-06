using ProductApi.Models;
using TiendaPromElec.DTOs;

namespace TiendaPromElec.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(long id);
        Task<Product> CreateAsync(ProductCreateDto dto);
        Task<bool> UpdateAsync(long id, ProductUpdateDto dto);
        Task<bool> DeleteAsync(long id);
    }
}
