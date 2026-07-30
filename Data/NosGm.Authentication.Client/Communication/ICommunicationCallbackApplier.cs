using System.Threading;
using System.Threading.Tasks;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Authentication.Client.Communication
{
    public interface ICommunicationCallbackApplier
    {
        Task ApplyAsync(
            WireV1.CommunicationCallbackEnvelope callback,
            CancellationToken cancellationToken);
    }
}
