using DCS_SR_Music.SRS_Helpers;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DCS_SR_Music.Network
{
    public class Broadcaster
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private IPEndPoint serverEndPoint;
        private UdpClient audioUdpClient;
        private bool stop = false;
        private readonly CancellationTokenSource pingStop = new CancellationTokenSource();
        private static readonly ConcurrentDictionary<string, MusicClient> musicClients = new ConcurrentDictionary<string, MusicClient>();

        // Events
        public event Action<bool, string> UpdateConnectionStatus;

        public bool SecureCoalitions { get; set; }
        public bool IsRunning { get; set; } = false;

        public Broadcaster(IPEndPoint endPoint, List<StationClient> stationClients)
        {
            serverEndPoint = new IPEndPoint(endPoint.Address, endPoint.Port);

            foreach (StationClient statClient in stationClients)
            {
                UpdateClientRadio(statClient);
            }
        }

        public void UpdateClientRadio(StationClient statClient)
        {
            string bluforGUID = statClient.BluforClient.ClientGuid;
            string opforGUID = statClient.OpforClient.ClientGuid;

            DCSPlayerRadioInfo bluforRadioInfo = statClient.BluforClient.RadioInfo;
            DCSPlayerRadioInfo opforRadioInfo = statClient.OpforClient.RadioInfo;

            // Frequencies, Encryptions, and Modulations should be the same between Blufor and Opfor
            var frequencies = new List<double>(1);
            var encryptions = new List<byte>(1);
            var modulations = new List<byte>(1);

            MusicClient bluforMusicClient;
            MusicClient opforMusicClient;

            if (bluforRadioInfo != null && opforRadioInfo != null)
            {
                frequencies.Add(bluforRadioInfo.radios[0].freq);
                encryptions.Add(bluforRadioInfo.radios[0].enc ? bluforRadioInfo.radios[0].encKey : (byte)0);
                modulations.Add((byte)bluforRadioInfo.radios[0].modulation);

                bluforMusicClient = new MusicClient
                {
                    GuidAsciiBytes = Encoding.ASCII.GetBytes(bluforGUID),
                    UnitId = bluforRadioInfo.unitId,
                    Frequencies = frequencies,
                    Encryptions = encryptions,
                    Modulations = modulations
                };

                opforMusicClient = new MusicClient
                {
                    GuidAsciiBytes = Encoding.ASCII.GetBytes(opforGUID),
                    UnitId = opforRadioInfo.unitId,
                    Frequencies = frequencies,
                    Encryptions = encryptions,
                    Modulations = modulations
                };
            }

            else
            {
                bluforMusicClient = new MusicClient
                {
                    GuidAsciiBytes = Encoding.ASCII.GetBytes(bluforGUID),
                };

                opforMusicClient = new MusicClient
                {
                    GuidAsciiBytes = Encoding.ASCII.GetBytes(opforGUID),
                };
            }

            musicClients[bluforGUID] = bluforMusicClient;
            musicClients[opforGUID] = opforMusicClient;
        }   

        public void Start()
        {
            audioUdpClient = new UdpClient();
            audioUdpClient.AllowNatTraversal(true);

            stop = false;
            StartPing();
            IsRunning = true;

            while (!stop)
            {
                try
                {
                    var groupEp = new IPEndPoint(IPAddress.Any, serverEndPoint.Port);
                    var bytes = audioUdpClient.Receive(ref groupEp);

                    if (bytes?.Length == 22)
                    {
                        Logger.Debug("Broadcaster - received ping back from server");
                    }
                }

                catch (Exception ex)
                {
                    // Swallow exception only if disconnect requested
                    if (!stop)
                    {
                        Logger.Error(ex, "Error listening for UDP ping back");
                        UpdateConnectionStatus(false, "client disconnected");
                    }
                }
            }
        }

        public void Stop()
        {
            try
            {
                pingStop.Cancel();
                stop = true;

                if (audioUdpClient != null)
                {
                    audioUdpClient.Close();
                    audioUdpClient = null;
                }

                IsRunning = false;
            }
            catch (Exception)
            {
                Logger.Error("Failed to close audio client");
            }
        }

        public void SendMusicPacket(string bluforGuid, string opforGuid, byte[] musicBytes)
        {
            try
            {
                if (!stop && (musicBytes != null))
                {
                    musicClients[bluforGuid].IsBroadcasting = true;
                    musicClients[bluforGuid].PacketNumber += 1;

                    MusicClient bluforClient = musicClients[bluforGuid];

                    // Generate packet
                    var udpVoicePacketBlufor = new UDPVoicePacket
                    {
                        GuidBytes = bluforClient.GuidAsciiBytes,
                        AudioPart1Bytes = musicBytes,
                        AudioPart1Length = (ushort) musicBytes.Length,
                        Frequencies = bluforClient.Frequencies.ToArray(),
                        UnitId = bluforClient.UnitId,
                        Encryptions = bluforClient.Encryptions.ToArray(),
                        Modulations = bluforClient.Modulations.ToArray(),
                        PacketNumber = bluforClient.PacketNumber
                    };

                    var encodedUdpVoicePacketBlufor = udpVoicePacketBlufor.EncodePacket();

                    // Send audio
                    audioUdpClient.Send(encodedUdpVoicePacketBlufor, encodedUdpVoicePacketBlufor.Length, serverEndPoint);
                    Logger.Debug($"Broadcaster sent blufor audio packet #{bluforClient.PacketNumber} for client {bluforGuid}");

                    if (SecureCoalitions)
                    {
                        musicClients[opforGuid].IsBroadcasting = true;
                        musicClients[opforGuid].PacketNumber += 1;

                        MusicClient opforClient = musicClients[opforGuid];

                        // Generate packet
                        var udpVoicePacketOpfor = new UDPVoicePacket
                        {
                            GuidBytes = opforClient.GuidAsciiBytes,
                            AudioPart1Bytes = musicBytes,
                            AudioPart1Length = (ushort)musicBytes.Length,
                            Frequencies = opforClient.Frequencies.ToArray(),
                            UnitId = opforClient.UnitId,
                            Encryptions = opforClient.Encryptions.ToArray(),
                            Modulations = opforClient.Modulations.ToArray(),
                            PacketNumber = opforClient.PacketNumber
                        };

                        var encodedUdpVoicePacketOpfor = udpVoicePacketOpfor.EncodePacket();

                        // Send audio
                        audioUdpClient.Send(encodedUdpVoicePacketOpfor, encodedUdpVoicePacketOpfor.Length, serverEndPoint);
                        Logger.Debug($"Broadcaster sent blufor audio packet #{opforClient.PacketNumber} for client {opforGuid}");
                    }
                }
            }

            catch (Exception ex)
            {
                Logger.Error(ex, "Error when building and sending music packet to server");
            }
        }

        private void StartPing()
        {
            Logger.Debug("Pinging Server - Starting");

            foreach (KeyValuePair<string, MusicClient> musicClientPair in musicClients)
            {
                byte[] message = Encoding.ASCII.GetBytes(musicClientPair.Key);

                // Force immediate ping once to avoid race condition before starting to listen
                audioUdpClient.Send(message, message.Length, serverEndPoint);
            }

            var thread = new Thread(() =>
            {
                // Wait for initial sync - then ping
                if (pingStop.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(2)))
                {
                    return;
                }

                while (!stop)
                {
                    Logger.Debug("Broadcaster - Pinging Server");

                    try
                    {
                        if (audioUdpClient != null)
                        {
                            foreach (KeyValuePair<string, MusicClient> musicClientPair in musicClients)
                            {
                                byte[] message = Encoding.ASCII.GetBytes(musicClientPair.Key);

                                if (!musicClientPair.Value.IsBroadcasting)
                                {
                                    audioUdpClient.Send(message, message.Length, serverEndPoint);
                                    Logger.Debug($"Broadcaster - pinging for music client: {musicClientPair.Value.UnitId}");
                                }
                            }
                        }
                    }

                    catch (Exception e)
                    {
                        Logger.Error(e, "Exception Sending Audio Ping! " + e.Message);
                    }

                    // Wait for cancel or quit
                    var cancelled = pingStop.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(15));

                    if (cancelled)
                    {
                        return;
                    }
                }
            });

            thread.IsBackground = true;
            thread.Start();
        }
    }
}
