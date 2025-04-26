using FixAPI.Entities;
using FixAPI.Repositories;

namespace FixAPI.Services
{
    public interface IExecutionReportService
    {
        Task<List<ExecutionReport>> GetUnprocessedReportsAsync(int executionReportThreshold);
        Task UpdateReportStatusAsync(int id, bool IsProcessed);
    }
    public class ExecutionReportService : IExecutionReportService
    {
        private readonly IServiceProvider _serviceProvider;

        public ExecutionReportService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<List<ExecutionReport>> GetUnprocessedReportsAsync(int executionReportThreshold)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IExecutionReportRepository>();
                return await repository.GetNewReportsAsync(executionReportThreshold);
            }
                
        }

        public async Task UpdateReportStatusAsync(int id, bool IsProcessed)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IExecutionReportRepository>();
                await repository.UpdateReportStatusAsync(id,IsProcessed);
            }
        }
    }
}
