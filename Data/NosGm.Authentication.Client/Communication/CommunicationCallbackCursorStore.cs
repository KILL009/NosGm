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

    public interface ICommunicationCallbackGenerationCursorStore
    {
        string RuntimeGenerationId { get; }

        ulong BindRuntimeGeneration(string runtimeGenerationId);
    }

    public sealed class FileCommunicationCallbackCursorStore
        : ICommunicationCallbackCursorStore,
          ICommunicationCallbackGenerationCursorStore
    {
        private const string CursorHeader = "NOSGM_CALLBACK_CURSOR_V1";
        private readonly object _syncRoot = new object();
        private readonly string _cursorPath;
        private string _runtimeGenerationId;

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

        public string RuntimeGenerationId
        {
            get
            {
                lock (_syncRoot)
                {
                    return _runtimeGenerationId ?? string.Empty;
                }
            }
        }

        public ulong BindRuntimeGeneration(string runtimeGenerationId)
        {
            if (!IsCanonicalNonEmptyGuid(runtimeGenerationId))
            {
                throw new ArgumentException(
                    "The callback runtime generation must be a canonical non-empty GUID.",
                    nameof(runtimeGenerationId));
            }

            lock (_syncRoot)
            {
                _runtimeGenerationId = runtimeGenerationId;
                return LoadBoundCursor();
            }
        }

        public ulong Load()
        {
            lock (_syncRoot)
            {
                return string.IsNullOrEmpty(_runtimeGenerationId)
                    ? 0
                    : LoadBoundCursor();
            }
        }

        public void Save(ulong sequence)
        {
            lock (_syncRoot)
            {
                if (string.IsNullOrEmpty(_runtimeGenerationId))
                {
                    throw new InvalidOperationException(
                        "The communication callback cursor has no bound runtime generation.");
                }

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
                    string text =
                        CursorHeader + "\n" +
                        _runtimeGenerationId + "\n" +
                        sequence.ToString(CultureInfo.InvariantCulture) + "\n";
                    byte[] payload = Encoding.ASCII.GetBytes(text);
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

        private ulong LoadBoundCursor()
        {
            if (!File.Exists(_cursorPath))
            {
                return 0;
            }

            string text = File.ReadAllText(
                _cursorPath,
                Encoding.ASCII).Trim();
            string[] lines = text.Split(
                new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);

            // Cursor files created before runtime-generation binding contained
            // only one unsigned sequence. They cannot be safely associated with
            // the current runtime, so migrate them by beginning at zero.
            if (lines.Length == 1 &&
                ulong.TryParse(
                    lines[0],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out _))
            {
                return 0;
            }

            if (lines.Length != 3 ||
                !string.Equals(
                    lines[0],
                    CursorHeader,
                    StringComparison.Ordinal) ||
                !IsCanonicalNonEmptyGuid(lines[1]) ||
                !ulong.TryParse(
                    lines[2],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out ulong sequence))
            {
                throw new InvalidOperationException(
                    "The communication callback cursor file is corrupt.");
            }

            return string.Equals(
                lines[1],
                _runtimeGenerationId,
                StringComparison.Ordinal)
                ? sequence
                : 0;
        }

        private static bool IsCanonicalNonEmptyGuid(string value)
        {
            return value != null &&
                   value.Length == 36 &&
                   Guid.TryParseExact(value, "D", out Guid parsed) &&
                   parsed != Guid.Empty &&
                   string.Equals(
                       parsed.ToString("D"),
                       value,
                       StringComparison.Ordinal);
        }
    }
}
