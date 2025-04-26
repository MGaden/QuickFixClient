using System.ComponentModel.DataAnnotations;

namespace FixAPI.Entities
{
    public class Order
    {
        [Key]
        public int Id { get; set; } // Primary key

        public string ClOrdID { get; set; }
        public string Symbol { get; set; }
        public char Side { get; set; } // 1 = Buy, 2 = Sell
        public decimal OrderQty { get; set; }
        public decimal Price { get; set; }
        public char OrdType { get; set; } // 1 = Market, 2 = Limit
        public string Account { get; set; }
        public string Currency { get; set; }
        public string? HandlInst { get; set; }
        public string? SecurityIDSource { get; set; }
        public string? SecurityID { get; set; }
        public string? TimeInForce { get; set; }
        public string TransactTime { get; set; }
        public string? SettlCurrency { get; set; }
        public string? ExDestination { get; set; }
        public string? ForexReq { get; set; }
        public string? SecurityExchange { get; set; }

        public string? OrigClOrdID { get; set; }
        public string? OrderID { get; set; }
        public string? MarketID { get; set; }
        public string? MarketSegmentID { get; set; }

        public string ClientName { get; set; }

        // New columns for tracking status, creation, and update times
        public bool IsActive { get; set; } = true; 
        public int RequestType { get; set; } // 1 = New, 2 = Replace, 3 = Cancel
        public DateTime CreationDate { get; set; } = DateTime.Now.Date;
        public DateTime CreationTime { get; set; } = DateTime.Now;
        public DateTime? UpdateTime { get; set; }
    }
}
