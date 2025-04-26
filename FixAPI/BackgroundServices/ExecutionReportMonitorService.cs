using FixAPI.Hubs;
using FixAPI.Services;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace FixAPI.BackgroundServices
{
    public class ExecutionReportMonitorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<ExecutionReportMonitorService> _logger;
        private readonly int _executionReportThreshold;
        private readonly int _serviceDelayMS;
        public ExecutionReportMonitorService(IHubContext<NotificationHub> hubContext, ILogger<ExecutionReportMonitorService> logger,
        IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _hubContext = hubContext;
            _logger = logger;
            // Read configuration values
            _executionReportThreshold = configuration.GetValue<int>("ExecutionReportMonitor:ExecutionReportThreshold");
            _serviceDelayMS = configuration.GetValue<int>("ExecutionReportMonitor:ServiceDelayMS");
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting ExecutionReportMonitorService.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var reportService = scope.ServiceProvider.GetRequiredService<IExecutionReportService>();

                        // Fetch new reports from the database
                        var newReports = await reportService.GetUnprocessedReportsAsync(_executionReportThreshold);

                        if (newReports.Any())
                        {
                            foreach (var report in newReports)
                            {
                                string message = $"New execution report available for order {report.OrderID}, client name: {report.ClientName}";

                                // Send notification via SignalR
                                await _hubContext.Clients.All.SendAsync("ReceiveAllNotification", message);
                                await _hubContext.Clients.Group(report.ClientName).SendAsync("ReceiveOrderNotification", message);

                                _logger.LogInformation("Notification sent for order {OrderID}", report.OrderID);

                                // Track this report as processed
                                await reportService.UpdateReportStatusAsync(report.Id, true);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing execution report changes.");
                }

                // Wait before polling the database again
                await Task.Delay(TimeSpan.FromMilliseconds(_serviceDelayMS), stoppingToken); // Adjust delay as needed
            }

            _logger.LogInformation("ExecutionReportMonitorService stopped.");
        }
    }
}