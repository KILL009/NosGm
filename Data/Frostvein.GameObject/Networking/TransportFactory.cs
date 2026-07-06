namespace Frostvein.GameObject
{
    public class TransportFactory
    {
        #region Instantiation

        private TransportFactory()
        {
            // do nothing
        }

        #endregion

        #region Properties

        public static TransportFactory Instance => _instance ?? (_instance = new TransportFactory());

        #endregion

        #region Methods

        public long GenerateTransportId()
        {
            _lastTransportId++;

            if (_lastTransportId >= long.MaxValue) _lastTransportId = 0;

            return _lastTransportId;
        }

        #endregion

        #region Members

        private static TransportFactory _instance;

        private long _lastTransportId = 100000;

        #endregion
    }
}