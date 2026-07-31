using System;

namespace NosGm.Communication.Client
{
    public static class CommunicationCallbackTypedEffectHandlerRegistry
    {
        private static readonly object SyncRoot = new object();
        private static Func<ICommunicationCallbackEnvelopeHandler>
            _createHandler;
        private static ICommunicationCallbackEnvelopeHandler _handler;

        public static bool Configure(
            Func<ICommunicationCallbackEnvelopeHandler> createHandler)
        {
            if (createHandler == null)
            {
                throw new ArgumentNullException(nameof(createHandler));
            }

            lock (SyncRoot)
            {
                if (_createHandler != null)
                {
                    return false;
                }

                _createHandler = createHandler;
                return true;
            }
        }

        public static ICommunicationCallbackEnvelopeHandler Resolve()
        {
            lock (SyncRoot)
            {
                if (_handler != null)
                {
                    return _handler;
                }
                if (_createHandler == null)
                {
                    return null;
                }

                _handler = _createHandler() ??
                    throw new InvalidOperationException(
                        "The typed callback effect handler factory returned no handler.");
                return _handler;
            }
        }
    }
}
