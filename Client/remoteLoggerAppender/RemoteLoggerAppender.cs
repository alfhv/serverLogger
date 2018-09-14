using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Filter;
using RestSharp;
using System;
using System.Configuration;
using System.Net.Http;
using System.Reflection;

namespace RemoteAppender
{
    [Serializable]
    public class LogMessage
    {
        public string Message { get; set; }
        public Level Level { get; set; }
        public string LoggerName { get; set; }

        public DateTime Timestamp { get; set; }
    }

    public class RemoteLoggerAppender : AppenderSkeleton
    {
        private const string ClientIdentifier = "clientIdentifier";

        //static ILog Logger = LogManager.GetLogger("RemoteLoggerAppender");
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        LoggingQueueDispatcher _dispatcher;
        int _errCount;
        int _count;
        string _remoteServerUrl;

        public RemoteLoggerAppender()
        {
            if (!InitializeAppender())
            {
                DiscardThisAppender();
            }
            else
            {
                _errCount = 0;
                _dispatcher = new LoggingQueueDispatcher(PostWithHttpClient);

                AddFilter(new LoggerMatchFilter { LoggerToMatch = "RemoteLoggerAppender", AcceptOnMatch = false });
            }
        }

        private bool InitializeAppender()
        {
            _remoteServerUrl = ConfigurationManager.AppSettings["loggerServerUrl"];

            if (string.IsNullOrEmpty(_remoteServerUrl))
            {
                Logger.Warn($"Remote server URL key <loggerServerUrl> not found for RemoteLoggerAppender...shooting down this appender");
                return false;
            }

            // check server is alive
            var client = new HttpClient();
            client.BaseAddress = new Uri(_remoteServerUrl);
            try
            {
                var result = client.GetAsync("api/logger/ping").Result;
                if (!result.IsSuccessStatusCode || result.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    // server is up, but not responding to ping
                    Logger.Warn($"Remote server for RemoteLoggerAppender not found or not responding ...shooting down this appender.");
                    return false;
                }
            }
            catch(Exception e)
            {
                // server is down
                Logger.Warn($"Remote server for RemoteLoggerAppender not found or not responding ...shooting down this appender.");
                return false;
            }

            return true;
        }

        public static void SetClientIdentifier(string clientIdentifier)
        {
            GlobalContext.Properties[ClientIdentifier] = clientIdentifier;
        }

        public static string GetClientIdentifier()
        {
            if (GlobalContext.Properties[ClientIdentifier] != null)
            {
                return GlobalContext.Properties[ClientIdentifier].ToString();
            }
            else if (GlobalContext.Properties["log4net:HostName"] != null)
            {
                return GlobalContext.Properties["log4net:HostName"].ToString();
            }
            else return "unknown";
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            if (!_dispatcher.IsActive)
                return;

            var logMsg = new LogMessage
            {
                Message = RenderLoggingEvent(loggingEvent),
                Level = loggingEvent.Level,
                LoggerName = GetClientIdentifier()
            };

            _dispatcher.Add(logMsg);
        }

        /// <summary>
        /// Send async log line to remote WebApi using System.Net.Http.HttpClient.
        /// </summary>
        /// <param name="logMsg"></param>
        private void PostWithHttpClient(LogMessage logMsg)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(_remoteServerUrl);

            try
            {
                _count++;
                var result = client.PutAsJsonAsync("api/logger/log", logMsg).Result;

                if (result.IsSuccessStatusCode && result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // all good sending the log, nothing to else to do
                }

                if (!result.IsSuccessStatusCode && _errCount < 5)
                {
                    // we try 5times, in case server be restarting or temporary overload.
                    _errCount++;
                    Logger.Warn($"Failed to send logs to remote server: {result.StatusCode}, {result.ReasonPhrase}");
                }
            }
            catch (Exception e)
            {
                if (_errCount < 5)
                {
                    _errCount++;

                    Logger.Warn($"Failed to send logs to remote server: {e.Message}");
                }
                else
                {
                    //AddFilter(new DenyAllFilter()); is better to discard the appender than just to silence it using filter
                    Logger.Warn($"Max error count reached. No more messages will be sent to remote log server.");

                    DiscardThisAppender();
                }
            }
        }
        
        private void DiscardThisAppender()
        {
            if (_dispatcher != null)
            {
                _dispatcher.Stop();
            }
            Close();
        }

        /// <summary>
        /// Send async log line to remote WebApi using RestSharp.RestClient.
        /// For some reason I forgot this way didnt work as expected.
        /// </summary>
        /// <param name="logMsg"></param>
        private async void LogToRemoteAsync(LogMessage logMsg)
        {
            var client = new RestClient("http://localhost:9000");

            var request = new RestRequest("api/logger/log", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddObject(logMsg);

            var response = await client.ExecuteTaskAsync(request);

            if (response.StatusCode != System.Net.HttpStatusCode.NoContent ||
                response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                // log local warning
            }
        }

        /// <summary>
        /// Send sync log line to remote WebApi using RestSharp.RestClient.
        /// For some reason I forgot this way didnt work as expected.
        /// </summary>
        /// <param name="logMessage"></param>
        private void LogToRemote(LogMessage logMessage)
        {
            var client = new RestClient("http://localhost:9000");

            var request = new RestRequest("api/logger/log", Method.POST);
            request.AddHeader("Accept", "application/json");
            request.Parameters.Clear();
            request.RequestFormat = DataFormat.Json;
            request.AddJsonBody(logMessage);
            //request.AddObject(logMsg);
            //request.AddParameter("application/json", logMessage, ParameterType.RequestBody);

            var response = client.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.NoContent ||
                response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                // log local warning
            }
        }
    }
}
