using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickFixClient.Models
{
    public class Order
    {
        public int Id { get; set; } // Primary key
        public string ClOrdID { get; set; }
        public string Symbol { get; set; }
        public char Side { get; set; } // 1 = Buy, 2 = Sell
        public decimal OrderQty { get; set; }
        public decimal Price { get; set; }
        public char OrdType { get; set; } // 1 = Market, 2 = Limit
        public string Account { get; set; } // 1
        public string Currency { get; set; } // 15
        public string HandlInst { get; set; } // 21
        public string SecurityIDSource { get; set; } // 22
        public string SecurityID { get; set; } // 48
        public string TimeInForce { get; set; } // 59
        public string TransactTime { get; set; } // 60
        public string SettlCurrency { get; set; } // 76
        public string ExDestination { get; set; } // 100
        public string ForexReq { get; set; } // 111 (optional)
        public string SecurityExchange { get; set; } // 207

        public string OrigClOrdID { get; set; }
        public string OrderID { get; set; }
        public string MarketID { get; set; }

        public string MarketSegmentID { get; set; }

        public string clientName { get; set; }
    }
}
