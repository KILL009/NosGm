// SPDX-License-Identifier: MIT

using System.Text;

namespace NosGM.Updater.Core;

public sealed class InstallLock : IDisposable
{
    private FileStream? _stream;

    private InstallLock(FileStream stream)
    {
        _stream = stream;
    }

    public static InstallLock Acquire(string installRoot)
    {
        var root = Path.GetFullPath(installRoot);
        Directory.CreateDirectory(root);
        var metadataRoot = InstallStateStore.GetMetadataRoot(root);
        Directory.CreateDirectory(metadataRoot);
        SafePaths.EnsureNoReparsePoints(root, metadataRoot);

        var lockPath = Path.Combine(metadataRoot, "update.lock");
        FileStream stream;
        try
        {
            stream = new FileStream(
                lockPath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.WriteThrough);
        }
        catch (IOException exception)
        {
            throw new InvalidOperationException(
                "Another NosGM launcher or recovery operation is using this installation.",
                exception);
        }

        try
        {
            var payload = Encoding.UTF8.GetBytes($"pid={Environment.ProcessId}{Environment.NewLine}");
            stream.SetLength(0);
            stream.Write(payload);
            stream.Flush(flushToDisk: true);
            return new InstallLock(stream);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _stream = null;
    }
}
