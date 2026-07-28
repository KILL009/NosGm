// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text;
using NosGM.Launcher;

if (OperatingSystem.IsWindows() is false)
{
    throw new PlatformNotSupportedException("Steam client preparation is Windows-only.");
}

var root = Path.Combine(Path.GetTempPath(), "NosGM-steam-selftest-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    RunSuccessfulPatch(Path.Combine(root, "success"));
    RunAmbiguousPatchRejection(Path.Combine(root, "ambiguous"));
    Console.WriteLine("NosGM Steam client patcher self-test passed.");
}
finally
{
    Directory.Delete(root, recursive: true);
}

static void RunSuccessfulPatch(string directory)
{
    Directory.CreateDirectory(directory);
    File.WriteAllBytes(Path.Combine(directory, "steam_api.dll"), [0x4D, 0x5A]);

    var originalPath = Path.Combine(directory, "NostaleClientX.exe");
    var originalBytes = CreateSyntheticClient("79.110.84.132");
    File.WriteAllBytes(originalPath, originalBytes);
    var originalHash = SHA256.HashData(originalBytes);

    if (!SteamClientPatcher.IsSteamInstallation(directory))
    {
        throw new InvalidOperationException("Steam installation detection failed.");
    }

    var result = SteamClientPatcher.Prepare(directory, "NostaleClientX.exe", "127.0.0.1");
    if (!File.Exists(result.ExecutablePath) || !File.Exists(result.StubPath))
    {
        throw new InvalidOperationException("Steam client preparation did not create both outputs.");
    }

    var originalAfter = File.ReadAllBytes(originalPath);
    if (!CryptographicOperations.FixedTimeEquals(originalHash, SHA256.HashData(originalAfter)))
    {
        throw new InvalidOperationException("Steam client preparation modified the original executable.");
    }

    var patched = File.ReadAllBytes(result.ExecutablePath);
    RequireBytes(patched, Encoding.ASCII.GetBytes("127.0.0.1"), "patched Login address");
    RequireBytes(patched, Encoding.ASCII.GetBytes("noscore_gf.dll\0"), "replacement wrapper import");
    ForbidBytes(patched, Encoding.ASCII.GetBytes("gf_wrapper.dll\0"), "original wrapper import");

    var lengthOffset = 32 + 4;
    if (BitConverter.ToInt32(patched, lengthOffset) != "127.0.0.1".Length)
    {
        throw new InvalidOperationException("Patched Delphi AnsiString length is incorrect.");
    }

    var stub = File.ReadAllBytes(result.StubPath);
    if (stub.Length < 256 || stub[0] != (byte)'M' || stub[1] != (byte)'Z')
    {
        throw new InvalidOperationException("Embedded Steam authentication stub is not a PE image.");
    }

    var peOffset = BitConverter.ToInt32(stub, 0x3C);
    if (peOffset <= 0 || peOffset + 6 >= stub.Length ||
        stub[peOffset] != (byte)'P' || stub[peOffset + 1] != (byte)'E' ||
        BitConverter.ToUInt16(stub, peOffset + 4) != 0x014C)
    {
        throw new InvalidOperationException("Steam authentication stub is not an x86 PE image.");
    }
}

static void RunAmbiguousPatchRejection(string directory)
{
    Directory.CreateDirectory(directory);
    File.WriteAllBytes(Path.Combine(directory, "steam_api.dll"), [0x4D, 0x5A]);
    var first = CreateSyntheticClient("79.110.84.132");
    var second = CreateIpCandidate("127.1.2.3");
    Array.Copy(second, 0, first, 256, second.Length);
    File.WriteAllBytes(Path.Combine(directory, "NostaleClientX.exe"), first);

    try
    {
        _ = SteamClientPatcher.Prepare(directory, "NostaleClientX.exe", "127.0.0.1");
        throw new InvalidOperationException("Ambiguous Steam client patch unexpectedly succeeded.");
    }
    catch (InvalidDataException)
    {
        // Expected: different IP-shaped constants are not patched blindly.
    }

    if (File.Exists(Path.Combine(directory, SteamClientPatcher.PatchedExecutableName)) ||
        File.Exists(Path.Combine(directory, SteamClientPatcher.StubFileName)))
    {
        throw new InvalidOperationException("Rejected Steam client patch left output files behind.");
    }
}

static byte[] CreateSyntheticClient(string embeddedAddress)
{
    var bytes = Enumerable.Repeat((byte)0x90, 512).ToArray();
    var candidate = CreateIpCandidate(embeddedAddress);
    Array.Copy(candidate, 0, bytes, 32, candidate.Length);
    Encoding.ASCII.GetBytes("gf_wrapper.dll\0").CopyTo(bytes, 160);
    return bytes;
}

static byte[] CreateIpCandidate(string address)
{
    var bytes = new byte[8 + 15];
    bytes[0] = 0xFF;
    bytes[1] = 0xFF;
    bytes[2] = 0xFF;
    bytes[3] = 0xFF;
    BitConverter.GetBytes(15).CopyTo(bytes, 4);
    Encoding.ASCII.GetBytes(address).CopyTo(bytes, 8);
    return bytes;
}

static void RequireBytes(byte[] haystack, byte[] needle, string description)
{
    if (FindBytes(haystack, needle) < 0)
    {
        throw new InvalidOperationException($"Missing {description}.");
    }
}

static void ForbidBytes(byte[] haystack, byte[] needle, string description)
{
    if (FindBytes(haystack, needle) >= 0)
    {
        throw new InvalidOperationException($"Unexpected {description}.");
    }
}

static int FindBytes(byte[] haystack, byte[] needle)
{
    for (var offset = 0; offset <= haystack.Length - needle.Length; offset++)
    {
        var matched = true;
        for (var index = 0; index < needle.Length; index++)
        {
            if (haystack[offset + index] == needle[index])
            {
                continue;
            }

            matched = false;
            break;
        }

        if (matched)
        {
            return offset;
        }
    }

    return -1;
}
