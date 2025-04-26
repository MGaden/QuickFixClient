using FixAPI.Models;
using FixAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace FixAPI.Controllers
{
    /// <summary>
    /// Handles file upload, download, and file listing operations in the MinIO bucket.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _fixService;
        private readonly IConfiguration _configuration;

        public OrderController(IOrderService fixService, IConfiguration configuration)
        {
            _fixService = fixService;
            _configuration = configuration;
        }

        //[AllowAnonymous]
        [HttpPost]
        [Route("sendNewOrder")]
        public async Task<IActionResult> SendNewOrder([FromBody] NewOrder order)
        {
            var clientName = getClientName(order.clientName);

            if (string.IsNullOrWhiteSpace(clientName))
            {
                return BadRequest("you are not authorize.");
            }

            if (order == null)
            {
                return BadRequest("Order object is required.");
            }

            try
            {
                await _fixService.SendNewOrderAsync(order, clientName);
                return Ok("Order sent to fix.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error sending order: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("sendReplaceOrder")]
        public async Task<IActionResult> SendReplaceOrder([FromBody] ReplaceOrder order)
        {
            var clientName = getClientName(order.clientName);

            if (string.IsNullOrWhiteSpace(clientName))
            {
                return BadRequest("you are not authorize.");
            }

            if (order == null)
            {
                return BadRequest("Order object is required.");
            }

            try
            {
                await _fixService.SendReplaceOrderAsync(order, clientName);
                return Ok("Replace order sent to fix.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error replace order: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("sendCancelOrder")]
        public async Task<IActionResult> SendCancelOrder([FromBody] CancelOrder order)
        {
            var clientName = getClientName(order.clientName);

            if (string.IsNullOrWhiteSpace(clientName))
            {
                return BadRequest("you are not authorize.");
            }

            if (order == null)
            {
                return BadRequest("Order object is required.");
            }

            try
            {
                await _fixService.SendCancelOrderAsync(order, clientName);
                return Ok("Cancel order sent to fix.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error cancel order: {ex.Message}");
            }
        }

        string getClientName(string clientName)
        {
            var token = HttpContext.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();
            if (token != null)
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var payload = jwtToken?.Payload as IDictionary<string, object>;
                return payload["client_id"].ToString();
            }

            return clientName;
        }

    }
}
