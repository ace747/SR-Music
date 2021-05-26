using DCS_SR_Music.SRS_Helpers;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DCS_SR_Music.Helpers
{
    public class NetworkMessage
    {
        public enum MessageType
        {
            UPDATE, // META Data update - No Radio Information
            PING,
            SYNC,
            RADIO_UPDATE, // Only received server side
            SERVER_SETTINGS,
            CLIENT_DISCONNECT, // Client disconnected
            VERSION_MISMATCH,
            EXTERNAL_AWACS_MODE_PASSWORD,
            EXTERNAL_AWACS_MODE_DISCONNECT
        }

        public SRClient Client { get; set; }

        public MessageType MsgType { get; set; }

        public List<SRClient> Clients { get; set; }

        public Dictionary<string, string> ServerSettings { get; set; }

        public string Version { get; set; }

        public string Encode()
        {
            Version = SRVersion.SRS_SUPPORTED_VERSION;
            return JsonConvert.SerializeObject(this) + "\n";
        }
    }
}
