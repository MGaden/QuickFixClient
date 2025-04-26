using QuickFix;
using QuickFix.Fields;
using QuickFix.FixValues;
using QuickFixClient.managers;
using QuickFixClient.Models;
using QuickFixClient.Services;
using System.Collections.Concurrent;
using System.Text.Json;

namespace QuickFixClient
{
    public class FixClientApp : MessageCracker, IApplication
    {
        public SessionID SessionID { get; private set; }

        private readonly SessionSettings _settings;
        private readonly IOrderService _orderService;

        public event Action OnDisconnect;
        public event Action OnReconnect;
        public bool IsDisconnected { get; private set; } = true; // Start with disconnected state


        // Cancellation token source to manage threads
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationTokenSource _sessionMonitorTokenSource;

        private int newOrderDelay = 100;
        private int NewOrdersBatchThreshold = 100; 
        public FixClientApp(SessionSettings settings, IOrderService orderService)
        {
            _settings = settings;
            _orderService = orderService;
            if (settings.Get().Has("NewOrdersDelayMS"))
            {
                newOrderDelay = int.Parse(settings.Get().GetString("NewOrdersDelayMS"));
            }
            if (settings.Get().Has("NewOrdersBatchThreshold"))
            {
                NewOrdersBatchThreshold = int.Parse(settings.Get().GetString("NewOrdersBatchThreshold"));
            }
        }

        public void OnCreate(SessionID sessionID)
        {
            SessionID = sessionID;
            Console.WriteLine("Session created: " + sessionID);
        }

        public void OnLogon(SessionID sessionID)
        {
            Console.WriteLine("Logon - SessionID: " + sessionID);

            //var session = Session.LookupSession(sessionID);
            //if (session != null)
            //{
            //    session.Reset("Sequence numbers have been reset");
            //    Console.WriteLine("Sequence numbers have been reset.");
            //}

            StartOrderProcessingThreads();

            IsDisconnected = false;
            OnReconnect?.Invoke(); // Raise reconnect event
        }

        public void OnLogout(SessionID sessionID)
        {
            Console.WriteLine("Logout - SessionID: " + sessionID);

            StopOrderProcessingThreads();

            IsDisconnected = true;
            OnDisconnect?.Invoke(); // Raise disconnect event
        }

        public void ToAdmin(Message message, SessionID sessionID) {

            if (message.Header.GetString(Tags.MsgType) == MsgType.LOGOUT)
            {
                Console.WriteLine("Logout requested. Stopping threads.");
                StopOrderProcessingThreads();
            }
        }

        public void ToApp(Message message, SessionID sessionID) { }

        public void FromAdmin(Message message, SessionID sessionID) {

            if (message.Header.GetString(Tags.MsgType) == MsgType.LOGOUT)
            {
                Console.WriteLine("Logout received from server. Stopping threads.");
                StopOrderProcessingThreads();
            }

        }

        public void FromApp(Message message, SessionID sessionID)
        {
            Console.WriteLine("Message received: " + message);
            if (message.Header.GetString(Tags.MsgType) == MsgType.EXECUTION_REPORT)
            {
                HandleExecutionReportAsync(message);
            }

            Crack(message, sessionID);
        }

        #region MessageCracker handlers
        public void OnMessage(QuickFix.FIX44.ExecutionReport m, SessionID s)
        {
            Console.WriteLine("Received execution report");
        }

        public void OnMessage(QuickFix.FIX44.OrderCancelReject m, SessionID s)
        {
            Console.WriteLine("Received order cancel reject");
        }
        #endregion

        // Start processing threads for new orders, place orders, and cancel orders
        private void StartOrderProcessingThreads()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            Task.Run(() => ProcessOrders(cancellationToken), cancellationToken);
            // Start threads for each type of order operation
            //Task.Run(() => ProcessNewOrders(cancellationToken), cancellationToken);
            //Task.Run(() => ProcessReplaceOrders(cancellationToken), cancellationToken);
            //Task.Run(() => ProcessCancelOrders(cancellationToken), cancellationToken);

            Console.WriteLine("Order processing threads started.");
        }

        // Stop processing threads by cancelling the token
        private void StopOrderProcessingThreads()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;

                Console.WriteLine("Order processing threads stopped.");
            }
        }

        // Method to process orders from the queue
        private async Task ProcessOrders(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Fetch new orders from database
                var newOrders = await _orderService.GetNotSentOrdersAsync(requestType: 1, NewOrdersBatchThreshold);
                var replaceOrders = await _orderService.GetNotSentOrdersAsync(requestType: 2, NewOrdersBatchThreshold);
                var cancelOrders = await _orderService.GetNotSentOrdersAsync(requestType: 3, NewOrdersBatchThreshold);

                if ((newOrders == null || newOrders.Count() == 0) && (newOrders == null || newOrders.Count() == 0) && (newOrders == null || newOrders.Count() == 0))
                {
                    await Task.Delay(newOrderDelay); // Small delay to prevent tight looping
                }
                // Enqueue new orders
                if (newOrders != null)
                {
                    foreach (var order in newOrders)
                    {
                        // Send new order
                        SendNewOrder(order);
                        await _orderService.UpdateOrderSentStatusAsync(order.Id, false);
                    }
                      
                }

                // Enqueue replace orders
                if (replaceOrders != null)
                {
                    foreach (var replaceOrder in replaceOrders)
                    {
                        // Send place order
                        SendReplaceOrder(replaceOrder);
                        await _orderService.UpdateOrderSentStatusAsync(replaceOrder.Id, false);
                    }
                }

                // Enqueue cancel orders
                if (cancelOrders != null)
                {
                    foreach (var cancelOrder in cancelOrders)
                    {
                        // Send cancel order
                        SendCancelOrder(cancelOrder);
                        await _orderService.UpdateOrderSentStatusAsync(cancelOrder.Id, false);
                    }
                }

            }
        }

        // Send a new order to the server
        private void SendNewOrder(Order order)
        {
            try
            {
                // Retrieve BeginString (FIX version) from the settings
                var beginString = _settings.Get(SessionID).GetString("BeginString");

                var newOrder = new Message();
                newOrder.Header.SetField(new QuickFix.Fields.BeginString(beginString)); // Dynamically set FIX version
                newOrder.Header.SetField(new MsgType(MsgType.ORDER_SINGLE));
                newOrder.SetField(new ClOrdID(order.ClOrdID));
                newOrder.SetField(new Symbol(order.Symbol));
                newOrder.SetField(new Side(order.Side)); // Side (1 = Buy, 2 = Sell)
                newOrder.SetField(new TransactTime(DateTime.Now)); // Transaction Time
                newOrder.SetField(new OrdType(order.OrdType)); // OrdType (1 = Market, 2 = Limit)

                // Optional fields
                newOrder.SetField(new OrderQty(order.OrderQty));
                newOrder.SetField(new Price(order.Price));

                // Set optional fields
                newOrder.SetField(new Account(order.Account)); // Account Number
                newOrder.SetField(new Currency(order.Currency)); // Currency
                newOrder.SetField(new HandlInst(order.HandlInst[0])); // Handling Instruction
                newOrder.SetField(new SecurityIDSource(order.SecurityIDSource)); // Security ID Source
                newOrder.SetField(new SecurityID(order.SecurityID)); // Security ID
                newOrder.SetField(new TimeInForce(order.TimeInForce[0])); // Time In Force
                newOrder.SetField(new SettlCurrency(order.SettlCurrency)); // Settlement Currency
                newOrder.SetField(new ExDestination(order.ExDestination)); // Execution Destination
                newOrder.SetField(new SecurityExchange(order.SecurityExchange)); // Security Exchange

                // Optional field with conditional check
                if (!string.IsNullOrEmpty(order.ForexReq))
                {
                    newOrder.SetField(new ForexReq(order.ForexReq[0] == '1')); // Forex Request, assuming "1" for true, "0" for false
                }

                try
                {
                    // Send the order to the target
                    Session.SendToTarget(newOrder, SessionID);
                    Console.WriteLine($"Order {order.ClOrdID} sent.");
                }
                catch (SessionNotFound ex)
                {
                    Console.WriteLine($"SessionNotFound: Failed to send order {order.ClOrdID}: {ex.Message}");
                    IsDisconnected = true;
                    OnDisconnect?.Invoke(); // Raise disconnect event if session is not found
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Send Exception: Failed to send order {order.ClOrdID}: {ex.Message}");
                    IsDisconnected = true;
                    OnDisconnect?.Invoke(); // Raise disconnect event if session is not found
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: Failed to send order {order.ClOrdID}: {ex.Message}");
            }
        }

        // Send a place order to the server
        private void SendReplaceOrder(Order order)
        {
            try
            {
                var placeOrderMessage = new Message();
                // Retrieve BeginString (FIX version) from the settings
                var beginString = _settings.Get(SessionID).GetString("BeginString");

                placeOrderMessage.Header.SetField(new QuickFix.Fields.BeginString(beginString)); // Dynamically set FIX version
                placeOrderMessage.Header.SetField(new MsgType(MsgType.ORDERCANCELREPLACEREQUEST));
                placeOrderMessage.SetField(new ClOrdID(order.ClOrdID));
                placeOrderMessage.SetField(new OrigClOrdID(order.OrigClOrdID)); // Original Client Order ID
                placeOrderMessage.SetField(new OrderID(order.OrderID));
                placeOrderMessage.SetField(new OrderQty(order.OrderQty));
                placeOrderMessage.SetField(new OrdType(order.OrdType)); // e.g., 2 = Limit
                placeOrderMessage.SetField(new Price(order.Price));
                placeOrderMessage.SetField(new SecurityID(order.SecurityID));
                placeOrderMessage.SetField(new SecurityIDSource(order.SecurityIDSource));
                placeOrderMessage.SetField(new Side(order.Side)); // Side (e.g., 1 = Buy)
                placeOrderMessage.SetField(new Symbol(order.Symbol));
                placeOrderMessage.SetField(new TimeInForce(order.TimeInForce[0])); // Time In Force
                placeOrderMessage.SetField(new TransactTime(DateTime.ParseExact(order.TransactTime, "yyyyMMdd-HH:mm:ss", null)));
                placeOrderMessage.SetField(new SettlCurrency(order.SettlCurrency));
                placeOrderMessage.SetField(new ExDestination(order.ExDestination));
                placeOrderMessage.SetField(new ForexReq(order.ForexReq[0] == '1'));

                try
                {
                    // Send the order to the target
                    Session.SendToTarget(placeOrderMessage, SessionID);
                    Console.WriteLine($"Replace order {order.ClOrdID} sent.");
                }
                catch (SessionNotFound ex)
                {
                    Console.WriteLine($"SessionNotFound: Failed to replace order {order.ClOrdID}: {ex.Message}");
                    IsDisconnected = true;
                    OnDisconnect?.Invoke(); // Raise disconnect event if session is not found
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Send Exception: Failed to replace order {order.ClOrdID}: {ex.Message}");
                    IsDisconnected = true;
                    OnDisconnect?.Invoke(); // Raise disconnect event if session is not found
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: Failed to replace order {order.ClOrdID}: {ex.Message}");
            }
        }

        // Send a cancel order to the server
        private void SendCancelOrder(Order order)
        {
            try
            {
                var cancelOrderMessage = new Message();
                // Retrieve BeginString (FIX version) from the settings
                var beginString = _settings.Get(SessionID).GetString("BeginString");

                cancelOrderMessage.Header.SetField(new QuickFix.Fields.BeginString(beginString)); // Dynamically set FIX version
                cancelOrderMessage.Header.SetField(new MsgType(MsgType.ORDERCANCELREQUEST));
                cancelOrderMessage.SetField(new ClOrdID(order.ClOrdID));
                cancelOrderMessage.SetField(new OrderID(order.OrderID)); // Order ID of the order to be canceled
                cancelOrderMessage.SetField(new Symbol(order.Symbol)); // Symbol
                cancelOrderMessage.SetField(new MarketID(order.MarketID));
                cancelOrderMessage.SetField(new MarketSegmentID(order.MarketSegmentID));
                
                try
                {
                    // Send the order to the target
                    Session.SendToTarget(cancelOrderMessage, SessionID);
                    Console.WriteLine($"Cancel order {order.ClOrdID} sent.");
                }
                catch (SessionNotFound ex)
                {
                    Console.WriteLine($"SessionNotFound: Failed to cancel order {order.ClOrdID}: {ex.Message}");
                    IsDisconnected = true;
                    OnDisconnect?.Invoke(); // Raise disconnect event if session is not found
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Send Exception: Failed to cancel order {order.ClOrdID}: {ex.Message}");
                    IsDisconnected = true;
                    OnDisconnect?.Invoke(); // Raise disconnect event if session is not found
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: Failed to cancel order {order.ClOrdID}: {ex.Message}");
            }
        }
        private async Task HandleExecutionReportAsync(Message message)
        {
            // Extract fields from ExecutionReport message
            var report = new QuickFixClient.Entities.ExecutionReport
            {
                OrderID = message.IsSetField(Tags.OrderID) ? message.GetString(Tags.OrderID) : string.Empty,
                ClOrdID = message.IsSetField(Tags.ClOrdID) ? message.GetString(Tags.ClOrdID) : string.Empty,
                ExecID = message.IsSetField(Tags.ExecID) ? message.GetString(Tags.ExecID) : string.Empty,
                ExecType = message.IsSetField(Tags.ExecType) ? message.GetChar(Tags.ExecType) : '0',
                OrdStatus = message.IsSetField(Tags.OrdStatus) ? message.GetChar(Tags.OrdStatus) : '0',
                Symbol = message.IsSetField(Tags.Symbol) ? message.GetString(Tags.Symbol) : string.Empty,
                Side = message.IsSetField(Tags.Side) ? message.GetChar(Tags.Side) : '0',
                LeavesQty = message.IsSetField(Tags.LeavesQty) ? message.GetDecimal(Tags.LeavesQty) : 0,
                CumQty = message.IsSetField(Tags.CumQty) ? message.GetDecimal(Tags.CumQty) : 0,
                AvgPx = message.IsSetField(Tags.AvgPx) ? message.GetDecimal(Tags.AvgPx) : 0,
                Account = message.IsSetField(Tags.Account) ? message.GetString(Tags.Account) : string.Empty,
                SecuritySubType = message.IsSetField(Tags.SecuritySubType) ? message.GetString(Tags.SecuritySubType) : string.Empty,
                OrderQty = message.IsSetField(Tags.OrderQty) ? message.GetDecimal(Tags.OrderQty) : 0,
                OrdType = message.IsSetField(Tags.OrdType) ? message.GetChar(Tags.OrdType) : '0',
                Price = message.IsSetField(Tags.Price) ? message.GetDecimal(Tags.Price) : 0,
                TimeInForce = message.IsSetField(Tags.TimeInForce) ? message.GetChar(Tags.TimeInForce) : '0',
                TransactTime = message.IsSetField(Tags.TransactTime) ? message.GetString(Tags.TransactTime) : string.Empty
            };

            if(!string.IsNullOrWhiteSpace(report.ClOrdID))
            {
                var oldOrder = await _orderService.GetOrderByClnOrderIdAsync(report.ClOrdID);
                if (oldOrder != null)
                {
                    report.ClientName = oldOrder.clientName;
                }
            }
            
            // Enqueue the report for background processing
            ReportQueueManager.Instance.AddReport(report);

            Console.WriteLine("Execution Report enqueued for processing.");
        }

    }
}
