using System.IO;
using System.Text;

namespace NosGm.Parser.Import
{
    public class ImportPackets : IImport
    {
        private readonly ImportConfiguration _configuration;

        public ImportPackets(ImportConfiguration configuration) => _configuration = configuration;

        private string FilePacket => Path.Combine(_configuration.PacketFolder, "packet.txt");

        public void Import()
        {
            using (var packetTxtStream = new StreamReader(FilePacket, Encoding.GetEncoding(1252)))
            {
                string line;
                while ((line = packetTxtStream.ReadLine()) != null)
                {
                    var linesave = line.Split(' ');
                    _configuration.Packets.Add(linesave);
                }
            }
        }
    }
}