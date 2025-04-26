using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
namespace FixAPI.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // Access the JWT token claims
            //var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

            var clientName = getClientName(Context);
            // Log or perform actions based on the token information
            Console.WriteLine($"Client connected: {clientName}");

            await base.OnConnectedAsync();

            await SubscribeToAccount(clientName);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var clientName = getClientName(Context);

            Console.WriteLine($"Client disconnected: {clientName}");

            await base.OnDisconnectedAsync(exception);

            await UnsubscribeFromAccount(clientName);
        }

        // Method for clients to subscribe to specific order notifications
        public async Task SubscribeToAccount(string account)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, account);
            await Clients.Caller.SendAsync("Subscribed", $"Subscribed to notifications for account {account}");
        }

        // Method to unsubscribe from order notifications if needed
        public async Task UnsubscribeFromAccount(string account)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, account);
            await Clients.Caller.SendAsync("Unsubscribed", $"Unsubscribed from notifications for account {account}");
        }

        // Method to send a notification to all clients subscribed to a specific order ID
        public async Task NotifyOrderUpdate(string account, string message)
        {
            await Clients.Group(account).SendAsync("ReceiveOrderNotification", message);
        }

        string getClientName(HubCallerContext context)
        {
            var token = context?.GetHttpContext()?.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();
            if (token != null)
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var payload = jwtToken?.Payload as IDictionary<string, object>;
                return payload["client_id"].ToString();
            }

            return "";
        }
    }
}
