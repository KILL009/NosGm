using System;
using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using System.Diagnostics;

namespace NosGm.DAL.EF.Interceptors
{
    public class SlowQueryInterceptor : DbCommandInterceptor
    {
        private readonly int _slowQueryThresholdMs;
        private const string StopwatchKey = "NosGm.DAL.EF.SlowQueryInterceptor.Stopwatch";
        private const string OperationTypeKey = "NosGm.DAL.EF.SlowQueryInterceptor.OperationType";

        public SlowQueryInterceptor()
        {
            if (int.TryParse(System.Configuration.ConfigurationManager.AppSettings["SlowQueryThresholdMs"], out int threshold))
            {
                _slowQueryThresholdMs = threshold;
            }
            else
            {
                _slowQueryThresholdMs = 50;
            }
        }

        public override void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            interceptionContext.SetUserState(StopwatchKey, Stopwatch.StartNew());
            interceptionContext.SetUserState(OperationTypeKey, "Reader");
            base.ReaderExecuting(command, interceptionContext);
        }

        public override void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            base.ReaderExecuted(command, interceptionContext);
            LogIfSlow(command, interceptionContext);
        }

        public override void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            interceptionContext.SetUserState(StopwatchKey, Stopwatch.StartNew());
            interceptionContext.SetUserState(OperationTypeKey, "NonQuery");
            base.NonQueryExecuting(command, interceptionContext);
        }

        public override void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            base.NonQueryExecuted(command, interceptionContext);
            LogIfSlow(command, interceptionContext);
        }

        public override void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            interceptionContext.SetUserState(StopwatchKey, Stopwatch.StartNew());
            interceptionContext.SetUserState(OperationTypeKey, "Scalar");
            base.ScalarExecuting(command, interceptionContext);
        }

        public override void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            base.ScalarExecuted(command, interceptionContext);
            LogIfSlow(command, interceptionContext);
        }

        private void LogIfSlow<TResult>(DbCommand command, DbCommandInterceptionContext<TResult> interceptionContext)
        {
            if (interceptionContext.FindUserState(StopwatchKey) is Stopwatch sw)
            {
                sw.Stop();
                if (sw.ElapsedMilliseconds > _slowQueryThresholdMs)
                {
                    string opType = interceptionContext.FindUserState(OperationTypeKey) as string ?? "Unknown";
                    string message = $"[Op: {opType}] Execution Time: {sw.ElapsedMilliseconds}ms. Command: {command.CommandText}";
                    SlowQueryLogWriter.Log(message);
                }
            }
        }
    }
}
