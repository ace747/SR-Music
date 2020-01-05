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

namespace DCS_SR_Music.Network
{
    public class StationClient
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly int stationNumber;
        private readonly IPEndPoint serverEndpoint;
        private static readonly int MAX_DECODE_ERRORS = 5;
        private bool sync = true;

        // Events
        public event Action<NetworkMessage> HandleMessage;
        public event Action<bool, string> UpdateConnectionStatus;

        public TcpClient BluforTcpClient { get; private set; }
        public TcpClient OpforTcpClient { get; private set; }
        public SRClient BluforClient { get; set; }
        public SRClient OpforClient { get; set; }

        public StationClient(int num, IPEndPoint endPoint)
        {
            stationNumber = num;
            serverEndpoint = endPoint;

            BluforClient = new SRClient
            {
                Name = "BLUFOR MUSIC CLIENT " + stationNumber.ToString(),
                Coalition = 2,
                ClientGuid = ShortGuid.NewGuid().ToString(),
                Position = new DcsPosition { x = 0, y = 0, z = 0 },
                LatLngPosition = new DCSLatLngPosition()
            };

            OpforClient = new SRClient
            {
                Name = "OPFOR MUSIC CLIENT " + stationNumber.ToString(),
                Coalition = 1,
                ClientGuid = ShortGuid.NewGuid().ToString(),
                Position = new DcsPosition { x = 0, y = 0, z = 0 },
                LatLngPosition = new DCSLatLngPosition()
            };
        }

        private void connectBluforClient()
        {
            using(BluforTcpClient = new TcpClient())
            {
                BluforTcpClient.SendTimeout = 10;

                try
                {
                    BluforTcpClient.NoDelay = true;

                    // Wait for 10 seconds before aborting connection attempt - no SRS server running/port opened in that case
                    BluforTcpClient.ConnectAsync(serverEndpoint.Address, serverEndpoint.Port).Wait(TimeSpan.FromSeconds(10));

                    if (BluforTcpClient.Connected)
                    {
                        Logger.Debug("Connected to server");
                        UpdateConnectionStatus(true, "");
                        bluforSync();
                    }

                    else
                    {
                        UpdateConnectionStatus(false, "connection attempt failed");
                        Logger.Warn($"Station {stationNumber.ToString()} BluforClient failed to connect to server @ {serverEndpoint.ToString()}");
                    }
                }

                catch (Exception ex)
                {
                   Logger.Error(ex, "Could not connect to server");
                   UpdateConnectionStatus(false, "connection attempt failed");
                }
            }
        }

        private void connectOpforClient()
        {
            using (OpforTcpClient = new TcpClient())
            {
                OpforTcpClient.SendTimeout = 10;

                try
                {
                    OpforTcpClient.NoDelay = true;

                    // Wait for 10 seconds before aborting connection attempt - no SRS server running/port opened in that case
                    OpforTcpClient.ConnectAsync(serverEndpoint.Address, serverEndpoint.Port).Wait(TimeSpan.FromSeconds(10));

                    if (OpforTcpClient.Connected)
                    {
                        Logger.Debug("Connected to server");
                        UpdateConnectionStatus(true, "");
                        opforSync();
                    }

                    else
                    {
                        UpdateConnectionStatus(false, "connection attempt failed");
                        Logger.Warn($"Station {stationNumber.ToString()} OpforClient failed to connect to server @ {serverEndpoint.ToString()}");
                    }
                }

                catch (Exception ex)
                {
                    Logger.Error(ex, "Could not connect to server");
                    UpdateConnectionStatus(false, "connection attempt failed");
                }
            }
        }

        private void disconnectBluforClient()
        {
            try
            {
                if (BluforTcpClient != null)
                {
                    BluforTcpClient.Close();
                }
            }

            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to disconnect from server");
            }

            Logger.Debug("Disconnecting from server");
        }

        private void disconnectOpforClient()
        {
            try
            {
                if (OpforTcpClient != null)
                {
                    OpforTcpClient.Close();
                }
            }

            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to disconnect from server");
            }

            Logger.Debug("Disconnecting from server");
        }

        private void bluforSync()
        {
            int decodeErrors = 0;

            try
            {
                using (var reader = new StreamReader(BluforTcpClient.GetStream(), Encoding.UTF8))
                {
                    try
                    {
                        // Start the loop off by sending a SYNC Request
                        sendMessage(new NetworkMessage
                        {
                            Client = BluforClient,
                            MsgType = NetworkMessage.MessageType.SYNC
                        });

                        string line;
                        while (sync && (line = reader.ReadLine()) != null)
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
                        if (sync)
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
                Logger.Error(ex, "Exception encountered during blufor client sync");
            }
        }

        private void opforSync()
        {
            int decodeErrors = 0;

            try
            {
                using (var reader = new StreamReader(OpforTcpClient.GetStream(), Encoding.UTF8))
                {
                    try
                    {
                        // Start the loop off by sending a SYNC Request
                        sendMessage(new NetworkMessage
                        {
                            Client = OpforClient,
                            MsgType = NetworkMessage.MessageType.SYNC
                        });

                        string line;
                        while (sync && (line = reader.ReadLine()) != null)
                        {
                            try
                            {
                                var message = JsonConvert.DeserializeObject<NetworkMessage>(line);
                                decodeErrors = 0; //reset counter

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

                    catch (Exception ex)
                    {
                        // Swallow exception only if disconnect requested
                        if (sync)
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
                Logger.Error(ex, "Exception encountered during opfor client sync");
            }
        }

        private void sendMessage(NetworkMessage message)
        {
            try
            {
                message.Version = SRVersion.VERSION;
                var json = message.Encode();
                var bytes = Encoding.UTF8.GetBytes(json);

                // Blufor coalition
                if (message.Client.Coalition == 2)
                {
                    BluforTcpClient.GetStream().Write(bytes, 0, bytes.Length);
                }

                // Opfor coalition
                else
                {
                    OpforTcpClient.GetStream().Write(bytes, 0, bytes.Length);
                }
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
            var bluforSyncThread = new Thread(connectBluforClient);
            var opforSyncThread = new Thread(connectOpforClient);

            bluforSyncThread.IsBackground = true;
            opforSyncThread.IsBackground = true;

            bluforSyncThread.Start();
            opforSyncThread.Start();
        }

        public void Disconnect()
        {
            sync = false;

            disconnectBluforClient();
            disconnectOpforClient();
        }

        public bool IsConnected()
        {
            try
            {
                if (BluforTcpClient != null && OpforTcpClient != null)
                {
                    if (BluforTcpClient.Connected && OpforTcpClient.Connected)
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
            DCSPlayerRadioInfo bluForRadioInfo = new DCSPlayerRadioInfo(freq, mod);
            bluForRadioInfo.name = BluforClient.Name;
            bluForRadioInfo.unit = BluforClient.Name;
            bluForRadioInfo.ptt = false;

            DCSPlayerRadioInfo opforRadioInfo = new DCSPlayerRadioInfo(freq, mod);
            opforRadioInfo.name = OpforClient.Name;
            opforRadioInfo.unit = OpforClient.Name;
            opforRadioInfo.ptt = false;

            // Update clients locally
            BluforClient.RadioInfo = bluForRadioInfo;
            OpforClient.RadioInfo = opforRadioInfo;

            // Update blufor radio on server
            sendMessage(new NetworkMessage
            {
                Client = BluforClient,
                MsgType = NetworkMessage.MessageType.RADIO_UPDATE
            });

            // Update opfor radio on server
            sendMessage(new NetworkMessage
            {
                Client = OpforClient,
                MsgType = NetworkMessage.MessageType.RADIO_UPDATE
            });

            return this;
        }
    }
}
