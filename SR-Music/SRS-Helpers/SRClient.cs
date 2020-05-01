using System.Net;

namespace DCS_SR_Music.SRS_Helpers
{
    public class SRClient
    {
        private float lineOfSightLoss; // 0.0 is NO Loss therefore Full line of sight

        public string ClientGuid { get; set; }

        public string Name { get; set; }

        public int Coalition { get; set; }

        public DCSPosition Position { get; set; }

        public DCSLatLngPosition LatLngPosition { get; set; }

        public long LastUpdate { get; set; }

        public DCSPlayerRadioInfo RadioInfo { get; set; }

        public bool Muted { get; set; }

        public IPEndPoint VoipPort { get; set; }

        public object ClientSession { get; set; }

        public float LineOfSightLoss
        {
            get
            {
                if (lineOfSightLoss == 0)
                {
                    return 0;
                }

                if ((Position.x == 0) && (Position.z == 0))
                {
                    return 0;
                }

                return lineOfSightLoss;
            }

            set { lineOfSightLoss = value; }
        }
    }
}