using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NosGm.Core
{
    public static class Logger
    {
        #region Properties

        public static ILog Log { get; set; }

        #endregion

        #region Methods

        /// <summary>
        ///     Writes development-only diagnostic information. Calls to this method
        ///     are removed from Release builds by the compiler.
        /// </summary>
        [Conditional("DEBUG")]
        public static void Debug(string data, [CallerMemberName] string memberName = "")
        {
            Log?.Debug($"[{memberName}]: {data}");
        }

        /// <summary>
        ///     Wraps up the error message with the CallerMemberName
        /// </summary>
        public static void Error(Exception ex, [CallerMemberName] string memberName = "")
        {
            Log?.Error($"[{memberName}]: {ex.Message}", ex);
        }

        /// <summary>
        ///     Wraps up the error message with the CallerMemberName
        /// </summary>
        public static void Error(string data, Exception ex = null, [CallerMemberName] string memberName = "")
        {
            if (ex != null)
            {
                Log?.Error($"[{memberName}]: {data} {ex.InnerException}", ex);
            }
            else
            {
                Log?.Error($"[{memberName}]: {data}");
            }
        }

        /// <summary>
        ///     Wraps up the fatal message with the CallerMemberName
        /// </summary>
        public static void Fatal(string data, Exception ex = null, [CallerMemberName] string memberName = "")
        {
            if (ex != null)
            {
                Log?.Fatal($"[{memberName}]: {data} {ex.InnerException}", ex);
            }
            else
            {
                Log?.Fatal($"[{memberName}]: {data}");
            }
        }

        /// <summary>
        ///     Wraps up the info message with the CallerMemberName
        /// </summary>
        public static void Info(string message, Exception ex = null, [CallerMemberName] string memberName = "")
        {
            if (ex != null)
            {
                Log?.Info(message, ex);
            }
            else
            {
                Log?.Info(message);
            }
        }

        public static void InitializeLogger(ILog log)
        {
            Log = log;

#if !DEBUG
            ConfigureProductionLogging(log);
#endif
        }

        /// <summary>
        ///     Wraps up the error message with the Logging Event
        /// </summary>
        public static void LogEvent(string logEvent, string data, Exception ex = null,
            [CallerMemberName] string memberName = "")
        {
            if (ex != null)
            {
                Log?.Info($"[{memberName}]: [{logEvent}]{data}", ex);
            }
            else
            {
                Log?.Info($"[{memberName}]: [{logEvent}]{data}");
            }
        }

        /// <summary>
        ///     Wraps up the error message with the Logging Event
        /// </summary>
        public static void LogEventError(string logEvent, string data, Exception ex = null,
            [CallerMemberName] string memberName = "")
        {
            if (ex != null)
            {
                Log?.Error($"[{memberName}]: [{logEvent}]{data}", ex);
            }
            else
            {
                Log?.Error($"[{memberName}]: [{logEvent}]{data}");
            }
        }

        /// <summary>
        ///     Wraps up the error message with the Logging Event
        /// </summary>
        public static void LogUserEvent(string logEvent, string caller, string data)
        {
            Log?.Info($"[{logEvent}][{caller}]{data}");
        }

        /// <summary>
        ///     Writes development-only user-event diagnostics.
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogUserEventDebug(string logEvent, string caller, string data)
        {
            Log?.Debug($"[{logEvent}][{caller}]{data}");
        }

        /// <summary>
        ///     Wraps up the error message with the Logging Event
        /// </summary>
        public static void LogUserEventError(string logEvent, string caller, string data, Exception ex)
        {
            Log?.Error($"[{logEvent}][{caller}]{data}", ex);
        }

        /// <summary>
        ///     Wraps up the warn message with the CallerMemberName
        /// </summary>
        public static void Warn(string data, Exception innerException = null, [CallerMemberName] string memberName = "")
        {
            if (innerException != null)
            {
                Log?.Warn($"[{memberName}]: {data} {innerException.InnerException}", innerException);
            }
            else
            {
                Log?.Warn($"[{memberName}]: {data}");
            }
        }

        private static void ConfigureProductionLogging(ILog log)
        {
            try
            {
                var hierarchy = LogManager.GetRepository() as Hierarchy;
                if (hierarchy == null)
                {
                    return;
                }

                hierarchy.Root.Level = Level.Info;

                RollingFileAppender mainFileAppender = hierarchy.GetAppenders()
                    .OfType<RollingFileAppender>()
                    .FirstOrDefault();

                if (mainFileAppender != null)
                {
                    string logPrefix = GetLogPrefix();
                    PatternLayout layout = CreateProductionLayout();

                    mainFileAppender.File = $"{logPrefix}.log";
                    mainFileAppender.AppendToFile = true;
                    mainFileAppender.RollingStyle = RollingFileAppender.RollingMode.Size;
                    mainFileAppender.MaxSizeRollBackups = 10;
                    mainFileAppender.MaxFileSize = 25L * 1024L * 1024L;
                    mainFileAppender.StaticLogFileName = true;
                    mainFileAppender.ImmediateFlush = false;
                    mainFileAppender.Layout = layout;
                    mainFileAppender.ActivateOptions();

                    EnsureCriticalErrorAppender(hierarchy, logPrefix);
                }

                hierarchy.Configured = true;
                log?.Info("[Logging] Production profile enabled: INFO level with bounded text-file rotation.");
            }
            catch (Exception ex)
            {
                log?.Error("[Logging] Failed to configure the production logging profile.", ex);
            }
        }

        private static void EnsureCriticalErrorAppender(Hierarchy hierarchy, string logPrefix)
        {
            const string appenderName = "CriticalErrorFileAppender";
            if (hierarchy.GetAppenders().Any(appender => appender.Name == appenderName))
            {
                return;
            }

            var errorAppender = new RollingFileAppender
            {
                Name = appenderName,
                File = $"{logPrefix}-error.log",
                AppendToFile = true,
                RollingStyle = RollingFileAppender.RollingMode.Size,
                MaxSizeRollBackups = 5,
                MaxFileSize = 10L * 1024L * 1024L,
                StaticLogFileName = true,
                ImmediateFlush = true,
                Threshold = Level.Error,
                Layout = CreateProductionLayout()
            };

            errorAppender.ActivateOptions();
            hierarchy.Root.AddAppender(errorAppender);
        }

        private static PatternLayout CreateProductionLayout()
        {
            var layout = new PatternLayout(
                "[%date{yyyy-MM-dd HH:mm:ss.fff}][%level][%thread] %message%newline%exception");
            layout.ActivateOptions();
            return layout;
        }

        private static string GetLogPrefix()
        {
            string friendlyName = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);
            if (string.IsNullOrWhiteSpace(friendlyName))
            {
                return "nosgm";
            }

            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            string safeName = new string(friendlyName
                .Select(character => invalidCharacters.Contains(character) ? '-' : character)
                .ToArray());

            return safeName.Replace('.', '-').ToLowerInvariant();
        }

        #endregion
    }
}
