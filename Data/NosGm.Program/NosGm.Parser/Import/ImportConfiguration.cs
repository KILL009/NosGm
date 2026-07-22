using System.Collections.Generic;

namespace NosGm.Parser.Import
{
    public class ImportConfiguration
    {
        public string Lang { get; set; }

        public string LangFolder { get; set; }

        public string DatFolder { get; set; }

        public string PacketFolder { get; set; }

        public string MapFolder { get; set; }

        public string Folder { get; set; }

        public List<string[]> Packets { get; set; }
    }
}