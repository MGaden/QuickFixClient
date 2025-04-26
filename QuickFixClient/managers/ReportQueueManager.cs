using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickFixClient.Entities;

namespace QuickFixClient.managers
{
    internal class ReportQueueManager
    {
        private static readonly Lazy<ReportQueueManager> _instance = new Lazy<ReportQueueManager>(() => new ReportQueueManager());

        // Thread-safe bag for execution reports
        private readonly ConcurrentBag<ExecutionReport> _executionReports = new ConcurrentBag<ExecutionReport>();

        private ReportQueueManager() { }

        public static ReportQueueManager Instance => _instance.Value;

        // Add a report to the list
        public void AddReport(ExecutionReport report)
        {
            _executionReports.Add(report);
        }

        // Fetch the current count of reports
        public int ReportCount => _executionReports.Count;

        // Retrieve and clear a batch of reports
        public List<ExecutionReport> FetchAndClearReports()
        {
            var reportList = _executionReports.ToList();

            // Clear the bag by recreating it, since ConcurrentBag has no built-in clear
            foreach (var report in reportList)
            {
                _executionReports.TryTake(out _);
            }

            return reportList;
        }
    }
}
