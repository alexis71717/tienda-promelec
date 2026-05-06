using ProductApi.Models;

namespace TiendaPromElec.Services
{
    public interface IOrderService
    {
        Task<IEnumerable<Order>> GetAllAsync();
        Task<Order?> GetByIdAsync(long id);
        Task<Order> CreateAsync(Order order);
        Task<bool> DeleteAsync(long id);
    }
}
