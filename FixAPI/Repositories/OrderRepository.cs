using FixAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace FixAPI.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> AddOrderAsync(Order order);
        Task<Order> GetOrderAsync(int orderId);
    }

    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Order> AddOrderAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order> GetOrderAsync(int orderId)
        {
            return await _context.Orders.FindAsync(orderId);
        }
    }
}
