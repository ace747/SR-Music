using DCS_SR_Music.SRS_Helpers;
using NLog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using DCS_SR_Music.Helpers;
using System.Collections.Concurrent;
using System.Threading;

namespace DCS_SR_Music.Network
{
    public class ClientSync
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly ConcurrentDictionary<string, SRClient> allClients = new ConcurrentDictionary<string, SRClient>();
        private bool versionMismatch = false;

        public List<StationClient> StationClients = new List<StationClient>();
        public string ServerVersion = "Unknown";
        public readonly SyncedServerSettings serverSettings = SyncedServerSettings.Instance;

        // Events
        public event Action<bool, string> UpdateConnectionStatus;

        public ClientSync(IPEndPoint endPoint)
        {
            StationClients.Add(new StationClient(0, endPoint));
            StationClients.Add(new StationClient(1, endPoint));
            StationClients.Add(new StationClient(2, endPoint));
            StationClients.Add(new StationClient(3, endPoint));

            StationClients[0].HandleMessage += HandleMessage;
            StationClients[1].HandleMessage += HandleMessage;
            StationClients[2].HandleMessage += HandleMessage;
            StationClients[3].HandleMessage += HandleMessage;
        }

        public void Connect()
        {
            foreach (StationClient statClient in StationClients)
            {
                new Thread(statClient.Connect).Start();
            }
        }

        public void Disconnect()
        {
            foreach (StationClient statClient in StationClients)
            {
                statClient.Disconnect();
            }
        }

        public bool AllClientsConnected()
        {
            if (StationClients[0].IsConnected() && StationClients[1].IsConnected() && StationClients[2].IsConnected() && StationClients[3].IsConnected())
            {
                Logger.Debug("All clients connected");
                return true;
            }

            else
            {
                return false;
            }
        }

        public bool IsStationClient(string Guid)
        {
            foreach (StationClient statClient in StationClients)
            {
                if (statClient.Client.ClientGuid == Guid)
                {
                    return true;
                }
            }

            return false;
        }

        public void UpdateStationClient(SRClient client)
        {
            foreach (StationClient statClient in StationClients)
            {
                if (statClient.Client.ClientGuid == client.ClientGuid)
                {
                    statClient.Client.LastUpdate = DateTime.Now.Ticks;
                    statClient.Client.Name = client.Name;
                    statClient.Client.Coalition = client.Coalition;
                    statClient.Client.Position = client.Position;
                    statClient.Client.LatLngPosition = client.LatLngPosition;
                }
            }
        }

        public void HandleMessage(NetworkMessage serverMessage)
        {
            try
            {
                switch (serverMessage.MsgType)
                {
                    // //////////////////////////// PING ////////////////////////////////
                    case NetworkMessage.MessageType.PING:
                        // Do nothing for now
                        break;

                    // //////////////////////////// RADIO UPDATE //////////////////////////
                    case NetworkMessage.MessageType.RADIO_UPDATE:
                        // Do nothing for now - only needed by server
                        break;

                    // //////////////////////////// EXTERNAL AWACS //////////////////////////
                    case NetworkMessage.MessageType.EXTERNAL_AWACS_MODE_PASSWORD:
                        // Do nothing for now
                        break;

                    case NetworkMessage.MessageType.EXTERNAL_AWACS_MODE_DISCONNECT:
                        // Do nothing for now
                        break;

                    // //////////////////////////// UPDATE ////////////////////////////////
                    case NetworkMessage.MessageType.UPDATE:
                        if (serverMessage.ServerSettings != null)
                        {
                            serverSettings.Decode(serverMessage.ServerSettings);
                        }

                        if (allClients.ContainsKey(serverMessage.Client.ClientGuid))
                        {
                            var srClient = allClients[serverMessage.Client.ClientGuid];
                            var updatedSrClient = serverMessage.Client;

                            if (srClient != null)
                            {
                                srClient.LastUpdate = DateTime.Now.Ticks;
                                srClient.Name = updatedSrClient.Name;
                                srClient.Coalition = updatedSrClient.Coalition;
                                srClient.Position = updatedSrClient.Position;
                                srClient.LatLngPosition = updatedSrClient.LatLngPosition;
                            }

                            if (IsStationClient(updatedSrClient.ClientGuid))
                            {
                                UpdateStationClient(updatedSrClient);
                            }
                        }

                        else
                        {
                            var connectedClient = serverMessage.Client;
                            connectedClient.LastUpdate = DateTime.Now.Ticks;
                            connectedClient.LineOfSightLoss = 0.0f;

                            allClients[serverMessage.Client.ClientGuid] = connectedClient;

                            Logger.Debug("Recevied New Client: " + NetworkMessage.MessageType.UPDATE + " From: " +
                            serverMessage.Client.Name + " Coalition: " + serverMessage.Client.Coalition);
                        }
                        break;

                    // //////////////////////////// SYNC ////////////////////////////////
                    case NetworkMessage.MessageType.SYNC:
                        Logger.Debug("Recevied: " + NetworkMessage.MessageType.SYNC);

                        // Check server version
                        if (serverMessage.Version == null)
                        {
                            Logger.Error("Disconnecting Unversioned Server");
                            UpdateConnectionStatus(false, "client disconnected");
                            break;
                        }

                        var serverVersion = Version.Parse(serverMessage.Version);
                        var protocolVersion = Version.Parse(SRVersion.MINIMUM_PROTOCOL_VERSION);
                        ServerVersion = serverMessage.Version;

                        if (serverVersion < protocolVersion)
                        {
                            Logger.Error($"Server version ({serverMessage.Version}) older than minimum procotol version ({SRVersion.MINIMUM_PROTOCOL_VERSION}) - disconnecting");
                            ShowVersionMistmatchWarning(serverMessage.Version);
                            UpdateConnectionStatus(false, "client disconnected");
                            break;
                        }

                        if (serverMessage.Clients != null)
                        {
                            foreach (var client in serverMessage.Clients)
                            {
                                if (IsStationClient(client.ClientGuid))
                                {
                                    UpdateStationClient(client);
                                }

                                client.LastUpdate = DateTime.Now.Ticks;
                                client.LineOfSightLoss = 0.0f;
                                allClients[client.ClientGuid] = client;
                            }
                        }

                        // Add server settings
                        serverSettings.Decode(serverMessage.ServerSettings);
                        HandleServerSettingsChange();
                        break;

                    // //////////////////////////// SERVER SETTINGS ////////////////////////////////
                    case NetworkMessage.MessageType.SERVER_SETTINGS:
                        Logger.Debug("Recevied: " + NetworkMessage.MessageType.SERVER_SETTINGS);

                        serverSettings.Decode(serverMessage.ServerSettings);
                        ServerVersion = serverMessage.Version;
                        HandleServerSettingsChange();
                        break;

                    // //////////////////////////// CLIENT DISCONNECT ////////////////////////////////
                    case NetworkMessage.MessageType.CLIENT_DISCONNECT:
                        Logger.Debug("Recevied: " + NetworkMessage.MessageType.CLIENT_DISCONNECT);

                        if (IsStationClient(serverMessage.Client.ClientGuid))
                        {
                            UpdateConnectionStatus(false, "client disconnected");
                        }

                        else
                        {
                            SRClient outClient;
                            allClients.TryRemove(serverMessage.Client.ClientGuid, out outClient);

                            Logger.Debug($"Disconnected client: ({outClient})");
                        }
                        break;

                    // //////////////////////////// VERSION MISMATCH ////////////////////////////////
                    case NetworkMessage.MessageType.VERSION_MISMATCH:
                        Logger.Error($"Version Mismatch Between Client ({SRVersion.SRS_SUPPORTED_VERSION}) & Server ({serverMessage.Version}) - Disconnecting");
                        
                        if (!versionMismatch)
                        {
                            ShowVersionMistmatchWarning(serverMessage.Version);
                        }

                        versionMismatch = true;
                        UpdateConnectionStatus(false, "client disconnected");
                        break;

                    default:
                        Logger.Warn("Recevied unknown message type");
                        break;
                }
            }

            catch (Exception ex)
            {
                Logger.Error(ex, "Exception encountered when handling server message");
            }
        }

        public void HandleServerSettingsChange()
        {
            // LOS_Enabled and Distance_Enabled are not supported since music is broadcasted from an arbitraty location
            if (serverSettings.GetSettingAsBool(ServerSettingsKeys.LOS_ENABLED))
            {
                Logger.Warn("Disconnecting client - line of sight not supported");
                UpdateConnectionStatus(false, "client disconnected");
                return;
            }

            if (serverSettings.GetSettingAsBool(ServerSettingsKeys.DISTANCE_ENABLED))
            {
                Logger.Warn("Disconnecting client - distance limit not supported");
                UpdateConnectionStatus(false, "client disconnected");
                return;
            }
        }

        public static void ShowVersionMistmatchWarning(string serverVersion)
        {
            MessageBox.Show($"The SRS server you're connecting to is incompatible with this SR Music Client. " +
                            $"\n\nServer Version: {serverVersion}" +
                            $"\nSupported Version: {SRVersion.SRS_SUPPORTED_VERSION}",
                            "SRS Server Incompatible",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
        }
    }
}
