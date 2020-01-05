using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCS_SR_Music.Network
{
    public class MusicClient
    {
        public byte[] GuidAsciiBytes { get; set; }
        public uint UnitId { get; set; }
        public ulong PacketNumber { get; set; } = 1;
        public List<double> Frequencies { get; set; }
        public List<byte> Encryptions { get; set; }
        public List<byte> Modulations { get; set; }
        public bool IsBroadcasting { get; set; } = false;
    }
}
