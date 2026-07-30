using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace NosGm.Communication.Client
{
    public interface ICommunicationCallbackCursorStore
    {
        ulong Load();

        void Save(ulong sequence);
    }

    public sealed class FileCommunicationCallbackCursorStore
        : ICommunicationCallbackCursorStore
    {
        private readonly object _syncRoot = new object();
        private readonly string _cursorPath;

        public FileCommunicationCallbackCursorStore(string cursorPath)
        {
            if (string.IsNullOrWhiteSpace(cursorPath) ||
                !Path.IsPathRooted(cursorPath))
            {
                throw new ArgumentException(
                    "The callback cursor path must be absolute.",
                    nameof(cursorPath));
            }

            _cursorPath = Path.GetFullPath(cursorPath);
        }

        public ulong Load()
        {
            lock (_syncRoot)
            {
                if (!File.Exists(_cursorPath))
                {
                    return 0;
                }

                string text = File.ReadAllText(
                    _cursorPath,
                    Encoding.ASCII).Trim();
                if (!ulong.TryParse(
                        text,
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out ulong sequence))
                {
                    throw new InvalidOperationException(
                        "The communication callback cursor file is corrupt.");
                }

                return sequence;
            }
        }

        public void Save(ulong sequence)
        {
            lock (_syncRoot)
            {
                string directory = Path.GetDirectoryName(_cursorPath);
                if (string.IsNullOrEmpty(directory))
                {
                    throw new InvalidOperationException(
                        "The callback cursor path has no parent directory.");
                }
                Directory.CreateDirectory(directory);

                string temporaryPath =
                    _cursorPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                string backupPath =
                    _cursorPath + "." + Guid.NewGuid().ToString("N") + ".bak";
                try
                {
                    byte[] payload = Encoding.ASCII.GetBytes(
                        sequence.ToString(CultureInfo.InvariantCulture) + "\n");
                    using (var stream = new FileStream(
                               temporaryPath,
                               FileMode.CreateNew,
                               FileAccess.Write,
                               FileShare.None,
                               4096,
                               FileOptions.WriteThrough))
                    {
                        stream.Write(payload, 0, payload.Length);
                        stream.Flush(true);
                    }

                    if (File.Exists(_cursorPath))
                    {
                        File.Replace(
                            temporaryPath,
                            _cursorPath,
                            backupPath,
                            true);
                        File.Delete(backupPath);
                    }
                    else
                    {
                        File.Move(temporaryPath, _cursorPath);
                    }
                }
                finally
                {
                    if (File.Exists(temporaryPath))
                    {
                        File.Delete(temporaryPath);
                    }
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }
                }
            }
        }
    }
}
