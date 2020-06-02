using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Songify.Models;

namespace Songify.Classes
{
    /// <summary>
    /// Log to a text file
    /// Runs in a seperate thread to not stop other processes
    /// </summary>
    class Log
    {
        private static Queue<LogMessage> logQueue;
        private static Thread logThread;
        private static bool run;
        private static readonly PathManager pathManager = new PathManager();

        /// <summary>
        /// Start the log Thread
        /// </summary>
        public void Start()
        {
            logQueue = new Queue<LogMessage>();
            logThread = new Thread(HandleLogQueue);
            run = true;
            logThread.Start();
            Add("Starting log");
        }
        
        /// <summary>
        /// Stop the log thread
        /// </summary>
        public void Stop()
        {
            Add("Stopping log");
            run = false;
            logThread.Abort();
        }

        /// <summary>
        /// Adds Message to Logging Queue
        /// MessageType defaults to "Normal"
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageType"></param>
        public void Add(string message, MessageType messageType = MessageType.Normal)
        {
            logQueue.Enqueue(new LogMessage(DateTime.Now, messageType, message));
        }

        /// <summary>
        /// Logs all elements in order to a text file
        /// </summary>
        private static void HandleLogQueue()
        {
            while (run)
            {
                try
                {
                    while (logQueue.Any())
                    {
                        try
                        {
                            LogMessage message = logQueue.Dequeue();
                            File.AppendAllText(pathManager.LogFilePath, message + Environment.NewLine);
                        }
                        catch
                        {
                            /* https://i.ytimg.com/vi/0oBx7Jg4m-o/maxresdefault.jpg */
                        }
                    }
                }
                finally
                {
                    Thread.Sleep(10);
                }
            }
        }
    }
}
