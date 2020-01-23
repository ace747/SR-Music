using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DCS_SR_Music.Network
{
    public class Session
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private bool sessionConnected = false;
        private bool disconnectAlreadyRequested = false;
        private object sessionStatusLock = new object();
        private IPEndPoint serverEndpoint;

        public bool Quit { get; set; } = false;
        public ClientSync ClientSyncer;
        public List<Station> Stations = new List<Station>();

        // Events
        public event Action<bool, string> ConnectionEvent;

        public Session(IPEndPoint endPoint)
        {
            serverEndpoint = endPoint;

            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);

            ClientSyncer = new ClientSync(endPoint);
            ClientSyncer.UpdateConnectionStatus += UpdateConnectionStatus;

            ClientSyncer.StationClients[0].UpdateConnectionStatus += UpdateConnectionStatus;
            ClientSyncer.StationClients[1].UpdateConnectionStatus += UpdateConnectionStatus;
            ClientSyncer.StationClients[2].UpdateConnectionStatus += UpdateConnectionStatus;
            ClientSyncer.StationClients[3].UpdateConnectionStatus += UpdateConnectionStatus;

            Stations.Add(new Station(0, dir, endPoint, ClientSyncer.StationClients[0].Client.ClientGuid));
            Stations.Add(new Station(1, dir, endPoint, ClientSyncer.StationClients[1].Client.ClientGuid));
            Stations.Add(new Station(2, dir, endPoint, ClientSyncer.StationClients[2].Client.ClientGuid));
            Stations.Add(new Station(3, dir, endPoint, ClientSyncer.StationClients[3].Client.ClientGuid));

            Stations[0].UpdateStationRadio += UpdateStationRadio;
            Stations[1].UpdateStationRadio += UpdateStationRadio;
            Stations[2].UpdateStationRadio += UpdateStationRadio;
            Stations[3].UpdateStationRadio += UpdateStationRadio;

            Stations[0].StationBroadcaster.UpdateConnectionStatus += UpdateConnectionStatus;
            Stations[1].StationBroadcaster.UpdateConnectionStatus += UpdateConnectionStatus;
            Stations[2].StationBroadcaster.UpdateConnectionStatus += UpdateConnectionStatus;
            Stations[3].StationBroadcaster.UpdateConnectionStatus += UpdateConnectionStatus;
        }

        public void Connect()
        {
            try
            {
                ClientSyncer.Connect();
            }

            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to connect session");
            }
        }

        public void Disconnect()
        {
            try
            {
                ClientSyncer.Disconnect();
            }

            catch (Exception ex)
            {
                Logger.Error(ex, "Error encountered when disconnecting session");
            }
        }

        public void UpdateConnectionStatus(bool connected, string message)
        {
            try
            {
                lock (sessionStatusLock)
                {
                    if (connected)
                    {
                        if (ClientSyncer.AllClientsConnected() && sessionConnected == false)
                        {
                            sessionConnected = true;
                            ConnectionEvent(true, "");
                            Logger.Info($"Connected to server @ {serverEndpoint.ToString()}");
                        }

                        else
                        {
                            return;
                        }
                    }

                    else
                    {
                        if (!Quit)
                        {
                            if (sessionConnected)
                            {
                                foreach (Station station in Stations)
                                {
                                    if (station.PlayingMusic)
                                    {
                                        station.StopMusic();
                                    }
                                }

                                // Wait for all clients to disconnect before signaling event
                                disconnectAlreadyRequested = true;
                                Task.Run(() => Disconnect()).Wait();

                                sessionConnected = false;
                                ConnectionEvent(false, message);
                                Logger.Info("Disconnnected from server");
                            }

                            else
                            {
                                if (!disconnectAlreadyRequested)
                                {
                                    disconnectAlreadyRequested = true;
                                    ConnectionEvent(false, message);
                                }
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to update connection status");
                ConnectionEvent(false, "connection failed");
            }

        }

        public void UpdateStationRadio(int stationNum, System.Double freq, int mod)
        {
            var statClient = ClientSyncer.StationClients[stationNum].UpdateRadioSettings(freq, mod);
            Stations[stationNum].StationBroadcaster.UpdateClientRadio(statClient);
        }

        public System.Double VerifyUniqueFrequency(int station, System.Double freq)
        {
            switch (station)
            {
                case 0:
                    if (freq == Stations[1].Frequency || freq == Stations[2].Frequency || freq == Stations[3].Frequency)
                    {
                        return Stations[0].Frequency;
                    }
                    break;

                case 1:
                    if (freq == Stations[0].Frequency || freq == Stations[2].Frequency || freq == Stations[3].Frequency)
                    {
                        return Stations[1].Frequency;
                    }
                    break;

                case 2:
                    if (freq == Stations[0].Frequency || freq == Stations[1].Frequency || freq == Stations[3].Frequency)
                    {
                        return Stations[2].Frequency;
                    }
                    break;

                // station 3
                default:
                    if (freq == Stations[0].Frequency || freq == Stations[1].Frequency || freq == Stations[2].Frequency)
                    {
                        return Stations[3].Frequency;
                    }
                    break;
            }

            return freq;
        }
    }
}
