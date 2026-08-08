using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using NosGm.Authentication.Server;
using NosGm.Authentication.Server.Security;
using NosGm.Authentication.Server.Services;
using NosGm.Authentication.Server.State;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
AuthenticationServerOptions options =
    AuthenticationServerOptions.Load(builder.Configuration);
CommunicationRuntimeOptions communicationOptions =
    CommunicationRuntimeOptions.Load(builder.Configuration);
ConfigurationRuntimeControlOptions configurationRuntimeControlOptions =
    ConfigurationRuntimeControlOptions.Load(builder.Configuration);
if (configurationRuntimeControlOptions.Enabled &&
    (!options.AllowedFingerprints.TryGetValue(
         WireV1.ClusterNodeRole.Master,
         out IReadOnlyCollection<string> masterFingerprints) ||
     masterFingerprints.Count == 0))
{
    throw new InvalidOperationException(
        "Configuration runtime control requires at least one Master mTLS certificate fingerprint.");
}
var roleMap = new ClientCertificateRoleMap(options);
var serverCertificate = options.LoadServerCertificate();
var trustedRootCertificate = options.LoadTrustedRootCertificate();

builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.Limits.Http2.MaxStreamsPerConnection =
        ClusterProtocolLimits.MaxConcurrentCallsPerConnection;
    kestrel.Listen(
        IPAddress.Loopback,
        options.Port,
        listen =>
        {
            listen.Protocols = HttpProtocols.Http1AndHttp2;
            listen.UseHttps(https =>
            {
                https.ServerCertificate = serverCertificate;
                https.ClientCertificateMode =
                    ClientCertificateMode.RequireCertificate;
                https.ClientCertificateValidation =
                    (certificate, chain, errors) =>
                        ValidateClientCertificate(
                            certificate,
                            errors,
                            trustedRootCertificate) &&
                        roleMap.IsKnownCertificate(certificate);
            });
        });
});

builder.Services.AddSingleton(options);
builder.Services.AddSingleton(communicationOptions);
builder.Services.AddSingleton(configurationRuntimeControlOptions);
builder.Services.AddSingleton(roleMap);
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<CommunicationCallbackRuntimeIdentity>();
builder.Services.AddSingleton<GameforgeAuthenticationState>();
builder.Services.AddSingleton<ClusterCommunicationState>();
builder.Services.AddSingleton<ConfigurationRuntimeController>();
builder.Services.AddSingleton<CommunicationCallbackHub>();
builder.Services.AddSingleton<CommunicationCallbackShadowWorldRegistry>();
builder.Services.AddSingleton<AuthenticationRequestReplayGuard>();
builder.Services.AddSingleton<AuthenticationDispatchGate>();
builder.Services.AddGrpc(grpc =>
{
    grpc.EnableDetailedErrors = false;
    grpc.MaxReceiveMessageSize =
        ClusterProtocolLimits.MaxInboundMessageBytes;
    grpc.MaxSendMessageSize =
        ClusterProtocolLimits.MaxOutboundMessageBytes;
});

WebApplication app = builder.Build();
app.UseGrpcWeb();
app.MapGrpcService<GameforgeAuthenticationService>().EnableGrpcWeb();
app.MapGrpcService<ClusterCommunicationService>().EnableGrpcWeb();
app.MapGrpcService<ClusterCommunicationCallbackService>().EnableGrpcWeb();
app.MapGrpcService<ClusterConfigurationService>().EnableGrpcWeb();
app.Lifetime.ApplicationStopped.Register(serverCertificate.Dispose);
if (trustedRootCertificate != null)
{
    app.Lifetime.ApplicationStopped.Register(
        trustedRootCertificate.Dispose);
}

CommunicationCallbackRuntimeIdentity callbackRuntimeIdentity =
    app.Services.GetRequiredService<CommunicationCallbackRuntimeIdentity>();
ConfigurationRuntimeStatus configurationRuntime =
    app.Services.GetRequiredService<ConfigurationRuntimeController>()
        .GetStatus();
app.Logger.LogInformation(
    "NosGM internal cluster runtime {InstanceId} callback generation {CallbackGenerationId} Configuration generation {ConfigurationGenerationId} control enabled {ConfigurationRuntimeControlEnabled} listening on loopback port {Port}; authentication, communication state, callback and shadow configuration services enabled.",
    options.InstanceId,
    callbackRuntimeIdentity.GenerationId,
    configurationRuntime.RuntimeGenerationId,
    configurationRuntime.ControlEnabled,
    options.Port);
app.Run();

static bool ValidateClientCertificate(
    X509Certificate2 certificate,
    SslPolicyErrors errors,
    X509Certificate2 trustedRootCertificate)
{
    if (certificate == null)
    {
        return false;
    }
    if (trustedRootCertificate == null)
    {
        return errors == SslPolicyErrors.None;
    }
    if ((errors & ~SslPolicyErrors.RemoteCertificateChainErrors) !=
        SslPolicyErrors.None)
    {
        return false;
    }

    using var chain = new X509Chain();
    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
    chain.ChainPolicy.CustomTrustStore.Add(trustedRootCertificate);
    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
    chain.ChainPolicy.DisableCertificateDownloads = true;
    bool trusted = chain.Build(certificate);
    if (!trusted)
    {
        Console.Error.WriteLine(
            "[TLS] Client certificate chain rejected: " +
            string.Join(
                ",",
                chain.ChainStatus.Select(
                    status => status.Status.ToString())));
    }
    return trusted;
}
