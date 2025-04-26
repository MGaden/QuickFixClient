using FiQuickFixClientxAPI.Repositories;
using Microsoft.Extensions.DependencyInjection;
using QuickFix.Fields;
using QuickFixClient.Models;
using QuickFixClient.Repositories;

namespace QuickFixClient.Services
{
    public interface IOrderService
    {
        Task<IEnumerable<Order>> GetNotSentOrdersAsync(int requestType, int batchSize);

        Task UpdateOrderSentStatusAsync(int orderId, bool isActive);

        Task<Order> GetOrderByClnOrderIdAsync(string clOrderId);
    }

    public class OrderService : IOrderService
    {
        private readonly IServiceProvider _serviceProvider;

        public OrderService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<IEnumerable<Order>> GetNotSentOrdersAsync(int requestType, int batchSize)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                var orders = await repository.GetNotSentOrdersAsync(requestType, batchSize);
                if (orders != null && orders.Count() > 0)
                {
                    List<Order> list = new List<Order>();
                    foreach (var order in orders)
                    {
                        list.Add(MapToNonDatabaseOrder(order));
                    }
                    return list;
                }
            }

            return null;
        }

        public async Task UpdateOrderSentStatusAsync(int orderId, bool isActive)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                await repository.UpdateOrderSentStatusAsync(orderId, isActive);
            }
        }

        public Order MapToNonDatabaseOrder(Entities.Order dbOrder)
        {
            if (dbOrder == null) throw new ArgumentNullException(nameof(dbOrder));

            return new Order
            {
                Id = dbOrder.Id,
                ClOrdID = dbOrder.ClOrdID,
                Symbol = dbOrder.Symbol,
                Side = dbOrder.Side,
                OrderQty = dbOrder.OrderQty,
                Price = dbOrder.Price,
                OrdType = dbOrder.OrdType,
                Account = dbOrder.Account,
                Currency = dbOrder.Currency,
                HandlInst = dbOrder.HandlInst,
                SecurityIDSource = dbOrder.SecurityIDSource,
                SecurityID = dbOrder.SecurityID,
                TimeInForce = dbOrder.TimeInForce,
                TransactTime = dbOrder.TransactTime,
                SettlCurrency = dbOrder.SettlCurrency,
                ExDestination = dbOrder.ExDestination,
                ForexReq = dbOrder.ForexReq,
                SecurityExchange = dbOrder.SecurityExchange,
                OrigClOrdID = dbOrder.OrigClOrdID,
                OrderID = dbOrder.OrderID,
                MarketID = dbOrder.MarketID,
                MarketSegmentID = dbOrder.MarketSegmentID,
                clientName = dbOrder.ClientName
            };
        }

        public async Task<Order> GetOrderByClnOrderIdAsync(string clOrderId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                var oldOrder = await repository.GetOrderByClnOrderIdAsync(clOrderId);
                return MapToNonDatabaseOrder(oldOrder);
            }
        }
    }
}
