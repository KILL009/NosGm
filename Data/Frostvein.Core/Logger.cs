using log4net;
using System;
using System.Runtime.CompilerServices;

namespace Frostvein.Core
{
    public static class Logger
    {
        #region Properties

        public static ILog Log { get; set; }

        #endregion

        #region Methods

        /// <summary>
        ///     Wraps up the error message with the CallerMemberName
        /// </summary>
        /// <param name="data"></param>
        /// <param name="memberName"></param>
        public static void Debug(string data, [CallerMemberName] string memberName = "")
        {
            Log?.Debug($"[{memberName}]: {data}");
        }

        /// <summary>
        ///     Wraps up the error message with the CallerMemberName
        /// </summary>
        /// <param name="memberName"></param>
        /// <param name="ex"></param>
        public static void Error(Exception ex, [CallerMemberName] string memberName = "")
        {
            Log?.Error($"[{memberName}]: {ex.Message}", ex);
        }

        /// <summary>
        ///     Wraps up the error message with the CallerMemberName
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ex"></param>
        /// <param name="memberName"></param>
        public static void Error(string data, Exception ex = null, [CallerMemberName] string memberName = "")
        {
            if (ex != null)
                Log?.Error($"[{memberName}]: {data} {ex.InnerException}", ex);
            else
                Log?.Error($"[{memberName}]: {data}");
        }

        /// <summary>
        ///     Wraps up the fatal message with the CallerMemberName
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ex"></param>
        /// <param name="memberName"></param>
        public static void Fatal(string data, Exception ex = null, [CallerMemberName] string memberName = "")
        {
            if (ex != null)
                Log?.Fatal($"[{memberName}]: {data} {ex.InnerException}", ex);
            else
                Log?.Fatal($"[{memberName}]: {data}");
        }

        /// <summary>
        ///     Wraps up the info message with the CallerMemberName
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        /// <param name="memberName"></param>
        public static void Info(string message, Exception ex = null, [CallerMemberName] string memberName = "")
        {
            if (ex != null)
                Log?.Info($"{message}", ex);
            else
                Log?.Info($"{message}");
        }

        public static void InitializeLogger(ILog log)
        {
            Log = log;
        }

        /// <summary>
        ///     Wraps up the error message with the Logging Event
        /// </summary>
        /// <param name="logEvent"></param>
        /// <param name="data"></param>
        /// <param name="ex"></param>
        /// <param name="memberName"></param>
        public static void LogEvent(string logEvent, string data, Exception ex = null,
            [CallerMemberName] string memberName = "")
        {
            if (ex != null)
                Log?.Info($"[{memberName}]: [{logEvent}]{data}");
            else
                Log?.Info($"[{memberName}]: [{logEvent}]{data}", ex);
        }

        /// <summary>
        ///     Wraps up the error message with the Logging Event
        /// </summary>
        /// <param name="logEvent"></param>
        /// <param name="data"></param>
        /// <param name="ex"></param>
        /// <param name="memberName"></param>
        public static void LogEventError(string logEvent, string data, Exception ex = null,
            [CallerMemberName] string memberName = "")
        {
            if (ex != null)
                Log?.Error($"[{memberName}]: [{logEvent}]{data}", ex);
            else
                Log?.Error($"[{memberName}]: [{logEvent}]{data}");
        }

        /// <summary>
        ///     Wraps up the error message with the Logging Event
        /// </summary>
        /// <param name="logEvent"></param>
        /// <param name="caller"></param>
        /// <param name="data"></param>
        public static void LogUserEvent(string logEvent, string caller, string data)
        {
            Log?.Info($"[{logEvent}][{caller}]{data}");
        }

        /// <summary>
        ///     Wraps up the message with the CallerMemberName
        /// </summary>
        /// <param name="logEvent"></param>
        /// <param name="caller"></param>
        /// <param name="data"></param>
        public static void LogUserEventDebug(string logEvent, string caller, string data)
        {
            Log?.Debug($"[{logEvent}][{caller}]{data}");
        }

        /// <summary>
        ///     Wraps up the error message with the Logging Event
        /// </summary>
        /// <param name="logEvent"></param>
        /// <param name="caller"></param>
        /// <param name="data"></param>
        /// <param name="ex"></param>
        public static void LogUserEventError(string logEvent, string caller, string data, Exception ex)
        {
            Log?.Error($"[{logEvent}][{caller}]{data}", ex);
        }

        /// <summary>
        ///     Wraps up the warn message with the CallerMemberName
        /// </summary>
        /// <param name="data"></param>
        /// <param name="innerException"></param>
        /// <param name="memberName"></param>
        public static void Warn(string data, Exception innerException = null, [CallerMemberName] string memberName = "")
        {
            if (innerException != null)
                Log?.Warn($"[{memberName}]: {data} {innerException.InnerException}", innerException);
            else
                Log?.Warn($"[{memberName}]: {data}");
        }

        #endregion
    }
}