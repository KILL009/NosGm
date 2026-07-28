using System.Threading;

namespace NosGm.Core
{
    public class SessionFactory
    {
        #region Instantiation

        private SessionFactory()
        {
        }

        #endregion

        #region Properties

        public static SessionFactory Instance => _instance ?? (_instance = new SessionFactory());

        #endregion

        #region Methods

        public int GenerateSessionId()
        {
            return Interlocked.Add(ref _sessionCounter, 2);
        }

        #endregion

        #region Members

        private static SessionFactory _instance;

        private int _sessionCounter;

        #endregion
    }
}
