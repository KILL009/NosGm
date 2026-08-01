using NosGm.LoggerService;
using System;
using System.Data;
using System.Data.Common;

namespace NosGm.DAL.EF.Helpers
{
    public static class DataAccessHelper
    {
        #region Members

        private static NosGmContext _context;

        #endregion

        #region Properties

        private static NosGmContext Context => _context ?? (_context = CreateContext());

        #endregion

        #region Methods

        /// <summary>
        ///     Begins and returns a new transaction. Be sure to commit/rollback/dispose this transaction
        ///     or use it in an using-clause.
        /// </summary>
        /// <returns>A new transaction.</returns>
        public static DbTransaction BeginTransaction()
        {
            // an open connection is needed for a transaction
            if (Context.Database.Connection.State == ConnectionState.Broken ||
                Context.Database.Connection.State == ConnectionState.Closed) Context.Database.Connection.Open();

            // begin and return new transaction
            return Context.Database.Connection.BeginTransaction();
        }

        /// <summary>
        ///     Creates new instance of database context.
        /// </summary>
        public static NosGmContext CreateContext()
        {
            return new NosGmContext();
        }

        /// <summary>
        ///     Disposes the current instance of database context.
        /// </summary>
        public static void DisposeContext()
        {
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
        }

        public static bool Initialize()
        {
            using (var context = CreateContext())
            {
                try
                {
                    context.Database.Initialize(true);
                    context.Database.Connection.Open();
                    //LoggerService.LogServer.Logger.LogAsync($"Database with Context {context} has been initialized", Domain.LogType.LOAD);
                }
                catch (Exception ex)
                {
                    string diagnostic = CreateInitializationDiagnostic(ex);
                    Console.Error.WriteLine(diagnostic);
                    try
                    {
                        LoggerService.LogServer.Logger.LogAsync(
                            diagnostic,
                            Domain.LogType.ERROR);
                    }
                    catch
                    {
                        // Database diagnostics must still reach the local process
                        // when the remote Log Server is unavailable.
                    }
                    return false;
                }

                return true;
            }
        }

        private static string CreateInitializationDiagnostic(Exception exception)
        {
            Exception root = exception?.GetBaseException() ?? exception;
            string outerMessage = NormalizeDiagnosticText(exception?.Message);
            string rootMessage = NormalizeDiagnosticText(root?.Message);
            string outerType = exception?.GetType().FullName ?? "UnknownException";
            string rootType = root?.GetType().FullName ?? outerType;
            int hResult = root?.HResult ?? exception?.HResult ?? 0;

            return "[DATABASE_INIT_FAILED] " +
                   "OuterType=" + outerType +
                   " OuterMessage=" + outerMessage +
                   " RootType=" + rootType +
                   " RootHResult=0x" + hResult.ToString("X8") +
                   " RootMessage=" + rootMessage;
        }

        private static string NormalizeDiagnosticText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "<empty>";
            }

            return value
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();
        }

        #endregion
    }
}