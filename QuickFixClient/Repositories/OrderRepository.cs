using QuickFixClient.Entities;
using Microsoft.EntityFrameworkCore;
using QuickFixClient.Repositories;

namespace FiQuickFixClientxAPI.Repositories
{
    public interface IOrderRepository
    {
        Task<IEnumerable<Order>> GetNotSentOrdersAsync(int requestType, int batchSize);
        Task<Order> GetOrderAsync(int orderId);
        Task UpdateOrderSentStatusAsync(int orderId, bool isActive);
        Task<Order> GetOrderByClnOrderIdAsync(string clOrderId);
    }

    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Order>> GetNotSentOrdersAsync(int requestType, int batchSize)
        {
            return await _context.Orders.Where(o => o.IsActive == true && o.RequestType == requestType).OrderBy(o => o.CreationTime).Take(batchSize).ToListAsync();
        }

        public async Task UpdateOrderSentStatusAsync(int orderId, bool isActive)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.IsActive = isActive;
                order.UpdateTime = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Order> GetOrderAsync(int orderId)
        {
            return await _context.Orders.FindAsync(orderId);
        }

        public async Task<Order> GetOrderByClnOrderIdAsync(string clOrderId)
        {
            return await _context.Orders.FirstOrDefaultAsync(o => o.ClOrdID == clOrderId);
        }
    }
}
