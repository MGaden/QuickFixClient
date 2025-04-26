using QuickFix;
using QuickFix.Fields;
using QuickFixServer.Models;
using System.Text.Json;

namespace QuickFixServer
{
    public class FixServerApp : MessageCracker, IApplication
    {
        private SessionID _sessionID;
        private readonly List<ExecutionReport> _executionReports;
        private Timer _executionReportTimer;
        private readonly SessionSettings _settings;

        public FixServerApp(SessionSettings settings)
        {
            _settings = settings;
            _executionReports = LoadExecutionReportsFromFile("data/execution_reports.json");
        }

        public void OnCreate(SessionID sessionID) { }

        public void OnLogon(SessionID sessionID)
        {
            _sessionID = sessionID;
            Console.WriteLine("Client logged in: " + sessionID);

            // Start sending execution reports every second
            _executionReportTimer = new Timer(SendExecutionReport, null, 0, 30000);
        }

        public void OnLogout(SessionID sessionID)
        {
            Console.WriteLine("Client logged out: " + sessionID);
            _executionReportTimer?.Dispose();
        }

        public void ToAdmin(Message message, SessionID sessionID) { }

        public void ToApp(Message message, SessionID sessionID)
        {
            Console.WriteLine("Sending message to client: " + message);
        }

        public void FromAdmin(Message message, SessionID sessionID) { }

        public void FromApp(Message message, SessionID sessionID)
        {
            Console.WriteLine("Message received from client: " + message);
            //Crack(message, sessionID);
        }

        private void SendExecutionReport(object state)
        {
            if (_sessionID == null || _executionReports.Count == 0)
                return;

            // Retrieve BeginString (FIX version) from the settings
            var beginString = _settings.Get(_sessionID).GetString("BeginString");

            foreach (var report in _executionReports)
            {
                var executionReport = new Message();
                executionReport.Header.SetField(new BeginString(beginString));
                executionReport.Header.SetField(new MsgType(MsgType.EXECUTION_REPORT));
                executionReport.SetField(new OrderID(report.OrderID));
                executionReport.SetField(new ClOrdID(report.ClOrdID));
                executionReport.SetField(new ExecID(report.ExecID));
                executionReport.SetField(new ExecType(report.ExecType));
                executionReport.SetField(new OrdStatus(report.OrdStatus));
                executionReport.SetField(new Symbol(report.Symbol));
                executionReport.SetField(new Side(report.Side));
                executionReport.SetField(new LeavesQty(report.LeavesQty));
                executionReport.SetField(new CumQty(report.CumQty));
                executionReport.SetField(new AvgPx(report.AvgPx));
                executionReport.SetField(new Account(report.Account));
                executionReport.SetField(new SecuritySubType(report.SecuritySubType));
                executionReport.SetField(new OrderQty(report.OrderQty));
                executionReport.SetField(new OrdType(report.OrdType[0]));
                executionReport.SetField(new Price(report.Price));
                executionReport.SetField(new TimeInForce(report.TimeInForce[0]));
                executionReport.SetField(new TransactTime(DateTime.ParseExact(report.TransactTime, "yyyyMMdd-HH:mm:ss.fff", null)));

                Session.SendToTarget(executionReport, _sessionID);
                Console.WriteLine("Execution Report sent to client.");
            }
        }

        private List<ExecutionReport> LoadExecutionReportsFromFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<List<ExecutionReport>>(json);
            }

            return new List<ExecutionReport>();
        }
    }
}
