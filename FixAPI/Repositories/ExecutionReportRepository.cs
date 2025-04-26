using FixAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace FixAPI.Repositories
{
    public interface IExecutionReportRepository
    {
        Task<List<ExecutionReport>> GetNewReportsAsync(int executionReportThreshold);
        Task UpdateReportStatusAsync(int id, bool IsProcessed);
    }
    public class ExecutionReportRepository : IExecutionReportRepository
    {
        private readonly AppDbContext _context;

        public ExecutionReportRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ExecutionReport>> GetNewReportsAsync(int executionReportThreshold)
        {
            return await _context.ExecutionReports
                .Where(report => report.IsProcessed == false || report.IsProcessed == null)
                .OrderBy(report => report.CreationTime).Take(executionReportThreshold)
                .ToListAsync();
        }

        public async Task UpdateReportStatusAsync(int id, bool IsProcessed)
        {
            var order = await _context.ExecutionReports.FindAsync(id);
            if (order != null)
            {
                order.IsProcessed = IsProcessed;
                order.LastUpdateDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }
    }
}
