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
                    LoggerService.LogServer.Logger.LogAsync($"There was an issue while loading the Database. It may not be up to date or non-existent.", Domain.LogType.ERROR);
                    return false;
                }

                return true;
            }
        }

        #endregion
    }
}