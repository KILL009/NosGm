using System;

namespace NosGm.Core
{
    public static class PacketLogSanitizer
    {
        public static string Describe(string packetContent)
        {
            if (packetContent == null)
            {
                return "Header=<null> Length=0 ContainsVerticalTab=False";
            }

            string header = ExtractHeader(packetContent);
            return $"Header={header} Length={packetContent.Length} " +
                   $"ContainsVerticalTab={packetContent.IndexOf('\v') >= 0}";
        }

        private static string ExtractHeader(string packetContent)
        {
            if (string.IsNullOrEmpty(packetContent))
            {
                return "<empty>";
            }

            int separator = packetContent.IndexOf(' ');
            string header = separator < 0 ? packetContent : packetContent.Substring(0, separator);
            if (header.Length > 24)
            {
                header = header.Substring(0, 24) + "...";
            }

            for (int i = 0; i < header.Length; i++)
            {
                char character = header[i];
                if (!char.IsLetterOrDigit(character) && character != '_' && character != '-' && character != '$')
                {
                    return "<invalid>";
                }
            }

            return header;
        }
    }
}
