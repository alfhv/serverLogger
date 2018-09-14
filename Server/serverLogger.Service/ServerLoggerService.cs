using log4net;
using log4net.Config;
using Microsoft.Owin.Hosting;
using System;
using System.Configuration;
using System.ServiceProcess;

namespace serverLogger.Service
{
    public partial class ServerLoggerService : ServiceBase
    {
        private IDisposable webApp;
        static ILog Logger = LogManager.GetLogger("ServerLoggerService");

        public ServerLoggerService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                var serverUrl = ConfigurationManager.AppSettings["loggerServerUrl"];
                webApp = WebApp.Start<StartOwin>(serverUrl);

                Logger.Info($"Server started at url: {serverUrl}");
            }
            catch(Exception e)
            {
                Logger.Fatal(e);
            }
        }

        protected override void OnStop()
        {
            Logger.Info("Server shutdown.");
            webApp.Dispose();
        }
    }
}
