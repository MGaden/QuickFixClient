using FixAPI.Entities;
using FixAPI.Models;
using FixAPI.Repositories;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace FixAPI.Services
{
    public interface IOrderService
    {
        Task<CreateOrderResponse> SendNewOrderAsync(NewOrder order, string clientName);

        Task<CreateOrderResponse> SendReplaceOrderAsync(ReplaceOrder order, string clientName);

        Task<CreateOrderResponse> SendCancelOrderAsync(CancelOrder order, string clientName);
    }

    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;

        public OrderService(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1); // Semaphore for thread safety

        public async Task SendNewOrderAsync(NewOrder order, string clientName, string filePath)
        {
            try
            {
                // Acquire the lock to ensure only one thread writes at a time
                await _fileLock.WaitAsync();

                List<NewOrder> orders;

                // Load existing orders if the file exists, otherwise initialize a new list
                if (File.Exists(filePath))
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    orders = JsonSerializer.Deserialize<List<NewOrder>>(json) ?? new List<NewOrder>();
                }
                else
                {
                    orders = new List<NewOrder>();
                }

                // Append the new order to the list
                orders.Add(order);

                // Serialize the list and overwrite the file
                var updatedJson = JsonSerializer.Serialize(orders, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, updatedJson);

                Console.WriteLine("New order sent successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending new order: {ex.Message}");
            }
            finally
            {
                // Release the lock
                _fileLock.Release();
            }
        }

        public async Task SendReplaceOrderAsync(ReplaceOrder order, string clientName, string filePath)
        {
            try
            {
                // Acquire the lock to ensure only one thread writes at a time
                await _fileLock.WaitAsync();

                List<ReplaceOrder> orders;

                // Load existing orders if the file exists, otherwise initialize a new list
                if (File.Exists(filePath))
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    orders = JsonSerializer.Deserialize<List<ReplaceOrder>>(json) ?? new List<ReplaceOrder>();
                }
                else
                {
                    orders = new List<ReplaceOrder>();
                }

                // Append the new order to the list
                orders.Add(order);

                // Serialize the list and overwrite the file
                var updatedJson = JsonSerializer.Serialize(orders, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, updatedJson);

                Console.WriteLine("Replace order sent successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending replace order: {ex.Message}");
            }
            finally
            {
                // Release the lock
                _fileLock.Release();
            }

            
        }

        public async Task SendCancelOrderAsync(CancelOrder order, string clientName, string filePath)
        {
            try
            {
                // Acquire the lock to ensure only one thread writes at a time
                await _fileLock.WaitAsync();

                List<CancelOrder> orders;

                // Load existing orders if the file exists, otherwise initialize a new list
                if (File.Exists(filePath))
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    orders = JsonSerializer.Deserialize<List<CancelOrder>>(json) ?? new List<CancelOrder>();
                }
                else
                {
                    orders = new List<CancelOrder>();
                }

                // Append the new order to the list
                orders.Add(order);

                // Serialize the list and overwrite the file
                var updatedJson = JsonSerializer.Serialize(orders, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, updatedJson);

                Console.WriteLine("Cancel order sent successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending cancel order: {ex.Message}");
            }
            finally
            {
                // Release the lock
                _fileLock.Release();
            }

            
        }

        public async Task<CreateOrderResponse> SendNewOrderAsync(NewOrder newOrder, string clientName)
        {
            // Map NewOrder to Order
            var order = new Order
            {
                ClOrdID = newOrder.ClOrdID,
                Symbol = newOrder.Symbol,
                Side = newOrder.Side,
                OrderQty = newOrder.OrderQty,
                Price = newOrder.Price,
                OrdType = newOrder.OrdType,
                Account = newOrder.Account,
                Currency = newOrder.Currency,
                HandlInst = newOrder.HandlInst,
                SecurityIDSource = newOrder.SecurityIDSource,
                SecurityID = newOrder.SecurityID,
                TimeInForce = newOrder.TimeInForce,
                TransactTime = newOrder.TransactTime,
                SettlCurrency = newOrder.SettlCurrency,
                ExDestination = newOrder.ExDestination,
                ForexReq = newOrder.ForexReq,
                SecurityExchange = newOrder.SecurityExchange,
                IsActive = true,
                RequestType = 1, // Assuming '1' for new order
                CreationDate = DateTime.Now.Date,
                CreationTime = DateTime.Now,
                ClientName = clientName
            };

            // Save the order
            var savedOrder = await _orderRepository.AddOrderAsync(order);

            // Map to CreateOrderResponse
            return new CreateOrderResponse
            {
                ClOrdID = savedOrder.ClOrdID,
                OrigClOrdID = savedOrder.OrigClOrdID,
                OrderID = savedOrder.OrderID,
                MarketID = savedOrder.MarketID
            };
        }

        public async Task<CreateOrderResponse> SendReplaceOrderAsync(ReplaceOrder newOrder, string clientName)
        {
            // Map NewOrder to Order
            var order = new Order
            {
                ClOrdID = newOrder.ClOrdID,
                Symbol = newOrder.Symbol,
                Side = newOrder.Side,
                OrderQty = newOrder.OrderQty,
                Price = newOrder.Price,
                OrdType = newOrder.OrdType,
                Account = newOrder.Account,
                Currency = newOrder.Currency,
                HandlInst = newOrder.HandlInst,
                SecurityIDSource = newOrder.SecurityIDSource,
                SecurityID = newOrder.SecurityID,
                TimeInForce = newOrder.TimeInForce,
                TransactTime = newOrder.TransactTime,
                SettlCurrency = newOrder.SettlCurrency,
                ExDestination = newOrder.ExDestination,
                ForexReq = newOrder.ForexReq,
                SecurityExchange = newOrder.SecurityExchange,
                IsActive = true,
                RequestType = 2, // Assuming '1' for new order
                CreationDate = DateTime.Now.Date,
                CreationTime = DateTime.Now,
                ClientName = clientName
            };

            // Save the order
            var savedOrder = await _orderRepository.AddOrderAsync(order);

            // Map to CreateOrderResponse
            return new CreateOrderResponse
            {
                ClOrdID = savedOrder.ClOrdID,
                OrigClOrdID = savedOrder.OrigClOrdID,
                OrderID = savedOrder.OrderID,
                MarketID = savedOrder.MarketID
            };
        }

        public async Task<CreateOrderResponse> SendCancelOrderAsync(CancelOrder newOrder, string clientName)
        {
            // Map NewOrder to Order
            var order = new Order
            {
                ClOrdID = newOrder.ClOrdID,
                MarketID = newOrder.MarketID,
                OrderID = newOrder.OrderID,
                MarketSegmentID = newOrder.MarketSegmentID,
                IsActive = true,
                RequestType = 3, // Assuming '1' for new order
                CreationDate = DateTime.Now.Date,
                CreationTime = DateTime.Now,
                ClientName = clientName
            };

            // Save the order
            var savedOrder = await _orderRepository.AddOrderAsync(order);

            // Map to CreateOrderResponse
            return new CreateOrderResponse
            {
                ClOrdID = savedOrder.ClOrdID,
                OrigClOrdID = savedOrder.OrigClOrdID,
                OrderID = savedOrder.OrderID,
                MarketID = savedOrder.MarketID
            };
        }
    }
}
