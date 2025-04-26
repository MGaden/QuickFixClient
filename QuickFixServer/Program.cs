using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;

namespace QuickFixServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var settings = new SessionSettings("config/server.cfg");
            //simple acceptor
            //var app = new FixServerApp(settings);
            //executer
            var app = new Executor();
            var storeFactory = new FileStoreFactory(settings);
            var logFactory = new FileLogFactory(settings);
            var messageFactory = new DefaultMessageFactory();
            IAcceptor acceptor = new ThreadedSocketAcceptor(app, storeFactory, settings, logFactory);

            acceptor.Start();
            Console.WriteLine("FIX server started. Press <Enter> to quit...");

            // Run the server until user stops it
            Console.ReadLine();
            acceptor.Stop();
        }
    }
}
