using FiQuickFixClientxAPI.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuickFix;
using QuickFix.Fields;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;
using QuickFixClient.managers;
using QuickFixClient.Models;
using QuickFixClient.Repositories;
using QuickFixClient.Services;
using System.Text.Json;
using QuickFixClient.Entities;

namespace QuickFixClient
{
    internal class Program
    {
        static int ExecutionReportBatchThreshold = 10;
        static int ExecutionReportBatchThresholdTimeoutS = 60;
        static int ExecutionReportDelayMS = 100;
        static async Task Main(string[] args)
        {
            // Build the configuration
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Set up dependency injection
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IConfiguration>(configuration);
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlServer(configuration.GetConnectionString("FixConnection")));

                    // Register repositories and services
                    services.AddScoped<IOrderRepository, OrderRepository>();
                    services.AddScoped<IOrderService, OrderService>();

                    services.AddScoped<IExecutionReportRepository, ExecutionReportRepository>();
                    services.AddScoped<IExecutionReportService, ExecutionReportService>();

                    // Register the FIX client dependencies
                    services.AddSingleton<SessionSettings>(new SessionSettings("config/client.cfg"));
                    services.AddSingleton<FixClientApp>();
                    services.AddSingleton<SocketInitiator>();
                })
                .Build();


            var storeFactory = new FileStoreFactory(new SessionSettings("config/client.cfg"));
            var logFactory = new FileLogFactory(new SessionSettings("config/client.cfg"));
            var messageFactory = new DefaultMessageFactory();

            var app = host.Services.GetRequiredService<FixClientApp>();
            var initiator = new SocketInitiator(app, storeFactory, new SessionSettings("config/client.cfg"), logFactory, messageFactory);

            // Create a CancellationTokenSource to manage the background threads
            using var cts = new CancellationTokenSource();
            var reportService = host.Services.GetRequiredService<IExecutionReportService>();
            // Start the background thread to process execution reports
            var reportProcessingTask = Task.Run(() => SaveReportsInBatchesAsync(reportService,cts.Token), cts.Token);


            // Subscribe to connection events
            app.OnDisconnect += () => StopFilling();
            app.OnReconnect += () => StartFilling();

            // Run the FIX client
            initiator.Start();

            Console.WriteLine("FIX client started. Press <Enter> to quit...");
            Console.ReadLine();

            initiator.Stop();
            cts.Cancel(); // Cancel all order loader tasks
            await reportProcessingTask; // Wait for the task to finish

        }

        private static void StartFilling()
        {
            Console.WriteLine("Reconnected. Resuming order queue filling.");

        }

        private static void StopFilling()
        {
            Console.WriteLine("Disconnected. Stopping order queue filling.");
        }


        // Background task to save reports in batches
        private static async Task SaveReportsInBatchesAsync(IExecutionReportService reportService, CancellationToken cancellationToken)
        {
            DateTime lastBatchSavedTime = DateTime.Now;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (ReportQueueManager.Instance.ReportCount >= ExecutionReportBatchThreshold ||
                    (DateTime.Now - lastBatchSavedTime).TotalSeconds >= ExecutionReportBatchThresholdTimeoutS) // Check batch threshold or timeout
                {
                    var reportsToSave = ReportQueueManager.Instance.FetchAndClearReports();

                    if (reportsToSave.Count > 0) // Only proceed if there are reports to save
                    {
                        var executionReports = reportsToSave.Select(report => new ExecutionReport
                        {
                            OrderID = report.OrderID,
                            ClOrdID = report.ClOrdID,
                            ExecID = report.ExecID,
                            ExecType = report.ExecType,
                            OrdStatus = report.OrdStatus,
                            Symbol = report.Symbol,
                            Side = report.Side,
                            LeavesQty = report.LeavesQty,
                            CumQty = report.CumQty,
                            AvgPx = report.AvgPx,
                            Account = report.Account,
                            SecuritySubType = report.SecuritySubType,
                            OrderQty = report.OrderQty,
                            OrdType = report.OrdType,
                            Price = report.Price,
                            TimeInForce = report.TimeInForce,
                            TransactTime = report.TransactTime,
                            ClientName = report.ClientName
                        }).ToList();

                        try
                        {
                            await reportService.SaveExecutionReportsInBatchAsync(executionReports);
                            Console.WriteLine($"{reportsToSave.Count} execution reports saved to the database.");
                            lastBatchSavedTime = DateTime.Now; // Reset the last batch save time
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error saving execution reports: {ex.Message}");
                        }
                    }
                }
                else
                {
                    // Wait briefly before checking the count and timeout again
                    await Task.Delay(ExecutionReportDelayMS, cancellationToken);
                }
            }

            Console.WriteLine("Report saving thread stopped.");
        }
    }
}
