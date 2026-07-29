using System.Net;
using System.Net.Security;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using NosGm.Authentication.Server;
using NosGm.Authentication.Server.Security;
using NosGm.Authentication.Server.Services;
using NosGm.Authentication.Server.State;
using NosGm.Cluster.Contracts.V1;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
AuthenticationServerOptions options =
    AuthenticationServerOptions.Load(builder.Configuration);
var roleMap = new ClientCertificateRoleMap(options);
var serverCertificate = options.LoadServerCertificate();

builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.Limits.Http2.MaxStreamsPerConnection =
        ClusterProtocolLimits.MaxConcurrentCallsPerConnection;
    kestrel.Listen(
        IPAddress.Loopback,
        options.Port,
        listen =>
        {
            listen.Protocols = HttpProtocols.Http2;
            listen.UseHttps(https =>
            {
                https.ServerCertificate = serverCertificate;
                https.ClientCertificateMode =
                    ClientCertificateMode.RequireCertificate;
                https.ClientCertificateValidation =
                    (certificate, chain, errors) =>
                        errors == SslPolicyErrors.None &&
                        roleMap.IsKnownCertificate(certificate);
            });
        });
});

builder.Services.AddSingleton(options);
builder.Services.AddSingleton(roleMap);
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<GameforgeAuthenticationState>();
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
app.MapGrpcService<GameforgeAuthenticationService>();
app.Lifetime.ApplicationStopped.Register(serverCertificate.Dispose);

app.Logger.LogInformation(
    "NosGM authentication runtime {InstanceId} listening on loopback port {Port}.",
    options.InstanceId,
    options.Port);
app.Run();
