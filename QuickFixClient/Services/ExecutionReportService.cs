using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using QuickFixClient.Entities;
using QuickFixClient.Repositories;

namespace QuickFixClient.Services
{
    public interface IExecutionReportService
    {
        Task SaveExecutionReportsInBatchAsync(IEnumerable<ExecutionReport> reports);
    }
    public class ExecutionReportService : IExecutionReportService
    {
        private readonly IServiceProvider _serviceProvider;

        public ExecutionReportService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task SaveExecutionReportsInBatchAsync(IEnumerable<ExecutionReport> reports)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IExecutionReportRepository>();
                await repository.SaveExecutionReportsAsync(reports);
            }
        }
    }
}
