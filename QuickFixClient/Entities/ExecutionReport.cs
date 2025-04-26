using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickFixClient.Entities
{
    public class ExecutionReport
    {
        public int Id { get; set; }
        public string OrderID { get; set; }
        public string ClOrdID { get; set; }
        public string ExecID { get; set; }
        public char ExecType { get; set; }
        public char OrdStatus { get; set; }
        public string Symbol { get; set; }
        public char Side { get; set; }
        public decimal LeavesQty { get; set; }
        public decimal CumQty { get; set; }
        public decimal AvgPx { get; set; }
        public string Account { get; set; }
        public string SecuritySubType { get; set; }
        public decimal OrderQty { get; set; }
        public char OrdType { get; set; }
        public decimal Price { get; set; }
        public char TimeInForce { get; set; }
        public string TransactTime { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.Now.Date;
        public DateTime CreationTime { get; set; } = DateTime.Now;
        public DateTime? LastUpdateDate { get; set; }
        public string ClientName { get; set; }

        public bool IsProcessed { get; set; } = false;
    }
}
