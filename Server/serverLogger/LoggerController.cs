using log4net;
using RemoteAppender;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace serverLogger
{
    public class LoggerController : ApiController
    {
        static ILog Logger = LogManager.GetLogger("Controller");

        LoggerService _loggerService;
        public LoggerController()
        {
            _loggerService = LoggerService.Instance;
        }

        [HttpGet]
        public IHttpActionResult Ping()
        {
            return Ok("pong");
        }

        [HttpPut]
        public IHttpActionResult Log(LogMessage logMessage)
        {
            Logger.Info($"Received log from: {logMessage.LoggerName}");

            _loggerService.AddLog(logMessage);

            return Ok();

            //await Task.Factory.StartNew((msg) =>
            //{
            //    var logMsg = msg as LogMessage;
            //    _loggerService.AddLog(logMsg);
            //    //_loggerService.GetLogger(logMsg.UserName).Logger.Log(_loggerService.GetType(), logMsg.Level, logMsg.Message, null);

            //    //Logger.Info(logMsg.Message);
            //}, logMessage);
        }

        // TODO: try to move all logic to async Tasks, but be carefull not to deadlock with async queue already in place(see: LoggingQueueDispatcher).

        //[HttpPut]
        //public async void LogAsync(LogMessage logMessage)
        //{
        //    await Task.Factory.StartNew((msg) =>
        //    {
        //        var logMsg = msg as LogMessage;
        //        _loggerService.AddLog(logMsg);
        //        //_loggerService.GetLogger(logMsg.UserName).Logger.Log(_loggerService.GetType(), logMsg.Level, logMsg.Message, null);

        //        //Logger.Info(logMsg.Message);
        //    }, logMessage);
        //}

        //[HttpPut]
        //public void LogSync(LogMessage logMessage)
        //{
        //    //Logger.Info($"Received: {logMessage.Message}");
        //    _loggerService.AddLog(logMessage);
        //}
    }
}
