// SPDX-License-Identifier: GPL-3.0-only

namespace NosGM.PacketCatalog;

internal static class SelfTest
{
    public static int Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "NosGM.PacketCatalog.SelfTest", Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "Data", "NosGm.Packets", "Packets", "ClientPackets"));
            Directory.CreateDirectory(Path.Combine(root, "Data", "NosGm.Handler"));

            File.WriteAllText(Path.Combine(root, "Data", "NosGm.Packets", "Packets", "ClientPackets", "Packets.cs"), """
using NosGm.Core;
namespace Synthetic.Packets;

[PacketHeader("hello", IsCharScreen = true)]
public class HelloPacket : PacketDefinition
{
    [PacketIndex(0)] public int Id { get; set; }
    [PacketIndex(1, serializeToEnd: true)] public string Text { get; set; }
}

[PacketHeader("broken")]
public class BrokenPacket : PacketDefinition
{
    [PacketIndex(0)] public int First { get; set; }
    [PacketIndex(0)] public int Duplicate { get; set; }
}
""");

            File.WriteAllText(Path.Combine(root, "Data", "NosGm.Handler", "Handlers.cs"), """
using Synthetic.Packets;
namespace Synthetic.Handlers;

public class Handler
{
    public void Handle(HelloPacket packet) { }

    [Packet("legacy")]
    public void Legacy(string packet) { }
}
""");

            var document = new CatalogAnalyzer(root).Analyze();
            Require(document.Summary.PacketTypes == 2, "Expected two packet classes.");
            Require(document.Summary.TypedHandlers == 1, "Expected one typed handler.");
            Require(document.Summary.RawHandlers == 1, "Expected one raw handler.");
            Require(document.Packets.Single(packet => packet.Name == "HelloPacket").Direction == "ClientToServer",
                "Typed packet direction was not resolved.");
            Require(document.Diagnostics.Any(diagnostic => diagnostic.Code == "PKT005" && diagnostic.Severity == CatalogSeverity.Error),
                "Duplicate PacketIndex diagnostic was not emitted.");

            var output = Path.Combine(root, "output");
            ReportWriter.WriteAll(document, output);
            Require(File.Exists(Path.Combine(output, "packet-catalog.json")), "JSON catalog was not written.");
            Require(File.Exists(Path.Combine(output, "PACKETS.md")), "Markdown catalog was not written.");
            Require(!File.ReadAllText(Path.Combine(output, "packet-catalog.json")).Contains("generatedAt", StringComparison.OrdinalIgnoreCase),
                "Catalog unexpectedly contains a generation timestamp.");

            Console.WriteLine("NosGM.PacketCatalog synthetic self-test passed.");
            return 0;
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
