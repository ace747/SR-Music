using DCS_SR_Music.Helpers;
using DCS_SR_Music.SRS_Helpers;
using DCS_SR_Music.Util;
using Newtonsoft.Json;
using NLog;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DCS_SR_Music.Network
{
    public class StationClient
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly int stationNumber;
        private readonly IPEndPoint serverEndpoint;
        private static readonly int MAX_DECODE_ERRORS = 5;
        private bool shouldSync = true;
        private TcpClient tcpClient;
        
        public SRClient Client { get; set; }

        // Events
        public event Action<NetworkMessage> HandleMessage;
        public event Action<bool, string> UpdateConnectionStatus;

        public StationClient(int num, IPEndPoint endPoint)
        {
            stationNumber = num;
            serverEndpoint = endPoint;

            Client = new SRClient
            {
                Name = "MUSIC CLIENT " + stationNumber.ToString(),
                Coalition = 2,
                ClientGuid = ShortGuid.NewGuid().ToString(),
                Position = new DcsPosition { x = 0, y = 0, z = 0 },
                LatLngPosition = new DCSLatLngPosition()
            };
        }

        private void connect()
        {
            using(tcpClient = new TcpClient())
            {
                tcpClient.SendTimeout = 10;

                try
                {
                    tcpClient.NoDelay = true;

                    // Wait for 10 seconds before aborting connection attempt - no SRS server running/port opened in that case
                    tcpClient.ConnectAsync(serverEndpoint.Address, serverEndpoint.Port).Wait(TimeSpan.FromSeconds(10));

                    if (tcpClient.Connected)
                    {
                        Logger.Debug("Connected to server");
                        UpdateConnectionStatus(true, "");
                        sync();
                    }

                    else
                    {
                        UpdateConnectionStatus(false, "connection attempt failed");
                        Logger.Warn($"Station {stationNumber.ToString()} Client failed to connect to server @ {serverEndpoint.ToString()}");
                    }
                }

                catch (Exception)
                {
                   Logger.Error($"Could not connect to server - station {stationNumber} client");
                   UpdateConnectionStatus(false, "connection attempt failed");
                }
            }
        }

        private void disconnect()
        {
            try
            {
                if (tcpClient != null)
                {
                    tcpClient.Close();
                }
            }

            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to disconnect from server");
            }

            Logger.Debug("Disconnecting from server");
        }

        private void sync()
        {
            int decodeErrors = 0;

            try
            {
                using (var reader = new StreamReader(tcpClient.GetStream(), Encoding.UTF8))
                {
                    try
                    {
                        // Start the loop off by sending a SYNC Request
                        sendMessage(new NetworkMessage
                        {
                            Client = Client,
                            MsgType = NetworkMessage.MessageType.SYNC
                        });

                        string line;
                        while (shouldSync && (line = reader.ReadLine()) != null)
                        {
                            try
                            {
                                var message = JsonConvert.DeserializeObject<NetworkMessage>(line);
                                decodeErrors = 0;

                                if (message != null)
                                {
                                    HandleMessage(message);
                                }
                            }

                            catch (Exception ex)
                            {
                                decodeErrors++;
                                Logger.Warn(ex, "Client exception reading from socket ");

                                if (decodeErrors > MAX_DECODE_ERRORS)
                                {
                                    UpdateConnectionStatus(false, "client disconnected");
                                    break;
                                }
                            }
                        }
                    }

                    // Swallow exception only if disconnect requested
                    catch (Exception ex)
                    {
                        if (shouldSync)
                        {
                            Logger.Error(ex, "Client exception during sync");
                        }
                    }
                }
            }

            catch (IOException)
            {
                Logger.Error("Exception encountered reading from socket during client sync");
            }

            catch (Exception ex)
            {
                Logger.Error(ex, "Exception encountered during client sync");
            }
        }

        private void sendMessage(NetworkMessage message)
        {
            try
            {
                message.Version = SRVersion.VERSION;
                var json = message.Encode();
                var bytes = Encoding.UTF8.GetBytes(json);

                tcpClient.GetStream().Write(bytes, 0, bytes.Length);
            }

            catch (Exception ex)
            {
                {
                    Logger.Error(ex, "Client exception sending to server");
                }

                UpdateConnectionStatus(false, "client disconnected");
            }
        }

        public void Connect()
        {
            connect();            
        }

        public void Disconnect()
        {
            shouldSync = false;

            disconnect();
        }

        public bool IsConnected()
        {
            try
            {
                if (tcpClient != null)
                {
                    if (tcpClient.Connected)
                    {
                        return true;
                    }
                }

                return false;
            }

            catch (Exception ex)
            {
                Logger.Error(ex, "Could not get client connection status");
                return false;
            }
        }

        public StationClient UpdateRadioSettings(System.Double freq, int mod)
        {
            // Radio Information for music broadcast
            DCSPlayerRadioInfo radioInfo = new DCSPlayerRadioInfo(freq, mod);
            radioInfo.name = Client.Name;
            radioInfo.unit = Client.Name;
            radioInfo.ptt = false;

            // Update clients locally
            Client.RadioInfo = radioInfo;

            // Update radio on server
            sendMessage(new NetworkMessage
            {
                Client = Client,
                MsgType = NetworkMessage.MessageType.RADIO_UPDATE
            });

            return this;
        }
    }
}
