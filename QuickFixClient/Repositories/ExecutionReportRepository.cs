using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickFixClient.Entities;

namespace QuickFixClient.Repositories
{
    public interface IExecutionReportRepository
    {
        Task SaveExecutionReportsAsync(IEnumerable<ExecutionReport> reports);
    }
    public class ExecutionReportRepository : IExecutionReportRepository
    {
        private readonly AppDbContext _context;

        public ExecutionReportRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task SaveExecutionReportsAsync(IEnumerable<ExecutionReport> reports)
        {
            await _context.ExecutionReports.AddRangeAsync(reports);
            await _context.SaveChangesAsync();
        }
    }
}
