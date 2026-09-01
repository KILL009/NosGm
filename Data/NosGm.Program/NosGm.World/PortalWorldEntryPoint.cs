using NosGm.Core;
using NosGm.GameObject;
using System;
using System.Threading.Tasks;

namespace NosGm.World
{
    /// <summary>
    /// Thin entry point around the existing World Program. The original startup runs first;
    /// Portal 2.0 workers are started only after the World Server has registered its channel.
    /// </summary>
    public static class PortalWorldEntryPoint
    {
        private static PortalBridgeWorker _portalBridge;
        private static ShopDeliveryWorker _shopDelivery;
        private static bool _shutdownHookRegistered;

        public static async Task Main(string[] args)
        {
            await Program.Main(args).ConfigureAwait(false);

            if (ServerManager.Instance.ChannelId <= 0)
            {
                return;
            }

            try
            {
                _shopDelivery = ShopDeliveryWorker.StartFromEnvironment();
                _portalBridge = PortalBridgeWorker.StartFromEnvironment();

                if ((_shopDelivery != null || _portalBridge != null) && !_shutdownHookRegistered)
                {
                    AppDomain.CurrentDomain.ProcessExit += (_, __) => StopWorkers();
                    _shutdownHookRegistered = true;
                }
            }
            catch (Exception exception)
            {
                Logger.Error("Portal 2.0 World integration failed to start", exception);
                StopWorkers();
            }
        }

        private static void StopWorkers()
        {
            try
            {
                _portalBridge?.Dispose();
            }
            catch (Exception exception)
            {
                Logger.Error("Portal bridge shutdown failed", exception);
            }

            try
            {
                _shopDelivery?.Dispose();
            }
            catch (Exception exception)
            {
                Logger.Error("NosMall delivery shutdown failed", exception);
            }

            _portalBridge = null;
            _shopDelivery = null;
        }
    }
}
