using log4net;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace RemoteAppender
{
    public class LoggingQueueDispatcher
    {
        static ILog Logger = LogManager.GetLogger("LoggingQueueDispatcher");

        private readonly BlockingCollection<LogMessage> _pendingMessages;
        private Thread _dispatcherProcess;

        // action to executed on every log message
        private Action<LogMessage> _actionWithLogMessage;
        private CancellationTokenSource _cancellationToken;
        public bool IsActive { get; private set; }

        public LoggingQueueDispatcher(Action<LogMessage> actionWithLogMessage) 
        {
            _pendingMessages = new BlockingCollection<LogMessage>(new ConcurrentQueue<LogMessage>());
            _actionWithLogMessage = actionWithLogMessage;
            _cancellationToken = new CancellationTokenSource();
            CreateBackgroundWorker();
        }

        /// <summary>
        /// Create the thread that will process BlockingCollection, so maybe is not so necessary to use async methods on API controller.
        /// </summary>
        private void CreateBackgroundWorker()
        {
            //Task.Factory.StartNew(MessageLoop, _cancellationToken);

            Thread thread = new Thread(MessageLoop);
            thread.Name = "LoggingQueueDispatcher Thread";
            thread.IsBackground = true;

            IsActive = true;
            thread.Start();

            _dispatcherProcess = thread;
            //_dispatcherProcess = Task.Factory.StartNew(() => MessageLoop(), new TaskCreationOptions);
        }

        private void MessageLoop()
        {
            foreach (var message in _pendingMessages.GetConsumingEnumerable(_cancellationToken.Token))
            {
                try
                {
                    _actionWithLogMessage(message);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Take operation was cancelled.");
                }
            }

            Logger.Info("no pending messages");
        }

        public void Add(LogMessage logMsg)
        {
            //logMsg.Timestamp = DateTime.Now;
            _pendingMessages.Add(logMsg);
        }

        public void Stop()
        {
            _cancellationToken.Cancel();
            _dispatcherProcess.Abort();
        }
    }
}
