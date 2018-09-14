using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using RemoteAppender;
using System.Configuration;
using System.IO;

namespace serverLogger
{
    public class LoggerService
    {
        static ILog Logger = LogManager.GetLogger("LoggerService"); // the WebApi logger, this is a separate file than client's log files

        LoggingQueueDispatcher _dispatcher;

        public LoggerService()
        {
            _dispatcher = new LoggingQueueDispatcher(Log);
        }

        private static LoggerService _instance;
        public static LoggerService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LoggerService();
                }
                return _instance;
            }
        }

        /// <summary>
        /// entry point from controller API
        /// </summary>
        /// <param name="logMsg"></param>
        public void AddLog(LogMessage logMsg)
        {
            _dispatcher.Add(logMsg); // forward log to dispatcher
        }

        /// <summary>
        /// Action that will be executed on every log message received from remote clients.
        /// This method will be called on dispatcher, when iterating through BlockingCollection
        /// </summary>
        /// <param name="logMsg"></param>
        private void Log(LogMessage logMsg)
        {
            GetLogger(logMsg.LoggerName).Logger.Log(this.GetType(), logMsg.Level, logMsg.Message, null);
        }

        private ILog GetLogger(string loggerName)
        {
            //It will create a repository for each different arg it will receive

            ILoggerRepository repository = null;

            var repositories = LogManager.GetAllRepositories();
            foreach (var loggerRepository in repositories)
            {
                if (loggerRepository.Name.Equals(loggerName))
                {
                    repository = loggerRepository;
                    break;
                }
            }

            Hierarchy hierarchy = null;
            if (repository == null)
            {
                //Create a new repository
                repository = LogManager.CreateRepository(loggerName);

                hierarchy = (Hierarchy)repository;
                hierarchy.Root.Additivity = false;

                //Add a rolling appender 
                var rollingAppender = GetRollingAppender(loggerName);
                hierarchy.Root.Level = Level.All;
                hierarchy.Root.AddAppender(rollingAppender);

                BasicConfigurator.Configure(repository);

                Logger.Info($"New logger created: {loggerName} => {(rollingAppender as RollingFileAppender).File}");
            }

            //Returns a logger with particular name;
            var logger = LogManager.GetLogger(loggerName, loggerName);

            return logger;
        }

        private IAppender GetRollingAppender(string arg)
        {
            var level = Level.All;

            var clientLayout = ConfigurationManager.AppSettings["loggerClientMsgLayout"];
            var outputFolder = ConfigurationManager.AppSettings["loggerOutputFolder"];
            
            var rollingFileAppenderLayout = new PatternLayout(clientLayout);
            rollingFileAppenderLayout.ActivateOptions();

            var rollingFileAppenderName =$"Rolling_{arg}";

            var rollingFileAppender = new RollingFileAppender();
            rollingFileAppender.Name = rollingFileAppenderName;
            rollingFileAppender.Threshold = level;
            rollingFileAppender.CountDirection = 0;
            rollingFileAppender.AppendToFile = true;
            rollingFileAppender.LockingModel = new FileAppender.MinimalLock();
            rollingFileAppender.StaticLogFileName = true;
            rollingFileAppender.RollingStyle = RollingFileAppender.RollingMode.Date;
            rollingFileAppender.DatePattern = ".yyyy-MM-dd'.log'";
            rollingFileAppender.Layout = rollingFileAppenderLayout;
            rollingFileAppender.File = Path.Combine(outputFolder, string.Format("{0}.{1}.log", "log", arg));
            rollingFileAppender.ActivateOptions();

            return rollingFileAppender;
        }
    }
}
