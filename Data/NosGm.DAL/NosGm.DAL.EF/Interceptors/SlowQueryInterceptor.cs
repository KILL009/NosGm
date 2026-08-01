using System;
using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using System.Diagnostics;

namespace NosGm.DAL.EF.Interceptors
{
    public class SlowQueryInterceptor : DbCommandInterceptor
    {
        private const int _slowQueryThresholdMs = 50;

        public override void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            interceptionContext.UserState = Stopwatch.StartNew();
            base.ReaderExecuting(command, interceptionContext);
        }

        public override void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            base.ReaderExecuted(command, interceptionContext);
            LogIfSlow(command, interceptionContext);
        }

        public override void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            interceptionContext.UserState = Stopwatch.StartNew();
            base.NonQueryExecuting(command, interceptionContext);
        }

        public override void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            base.NonQueryExecuted(command, interceptionContext);
            LogIfSlow(command, interceptionContext);
        }

        public override void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            interceptionContext.UserState = Stopwatch.StartNew();
            base.ScalarExecuting(command, interceptionContext);
        }

        public override void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            base.ScalarExecuted(command, interceptionContext);
            LogIfSlow(command, interceptionContext);
        }

        private void LogIfSlow<TResult>(DbCommand command, DbCommandInterceptionContext<TResult> interceptionContext)
        {
            if (interceptionContext.UserState is Stopwatch sw)
            {
                sw.Stop();
                if (sw.ElapsedMilliseconds > _slowQueryThresholdMs)
                {
                    string message = $"[SLOW QUERY] Execution Time: {sw.ElapsedMilliseconds}ms. Command: {command.CommandText}";
                    _ = NosGm.LoggerService.LogServer.Logger.LogAsync(message, NosGm.Domain.LogType.ERROR);
                }
            }
        }
    }
}
