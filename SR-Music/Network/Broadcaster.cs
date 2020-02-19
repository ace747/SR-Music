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
        private int stationNumber;
        private IPEndPoint serverEndPoint;
        private UdpClient audioUdpClient;
        private bool stop = false;
        private readonly CancellationTokenSource pingStop = new CancellationTokenSource();
        private MusicClient musicClient;

        // Events
        public event Action<bool, string> UpdateConnectionStatus;
        public bool IsRunning { get; set; } = false;

        public Broadcaster(int statNumber, IPEndPoint endPoint)
        {
            stationNumber = statNumber;
            serverEndPoint = new IPEndPoint(endPoint.Address, endPoint.Port);
        }

        public void UpdateClientRadio(StationClient statClient)
        {
            DCSPlayerRadioInfo radioInfo = statClient.Client.RadioInfo;

            var frequencies = new List<double>(1);
            var encryptions = new List<byte>(1);
            var modulations = new List<byte>(1);

            if (radioInfo != null)
            {
                frequencies.Add(radioInfo.radios[0].freq);
                encryptions.Add(radioInfo.radios[0].enc ? radioInfo.radios[0].encKey : (byte)0);
                modulations.Add((byte)radioInfo.radios[0].modulation);

                musicClient.UnitId = radioInfo.unitId;
                musicClient.Frequencies = frequencies;
                musicClient.Encryptions = encryptions;
                musicClient.Modulations = modulations;
            }
        }   

        public void Start(string guid)
        {
            musicClient = new MusicClient
            {
                GuidAsciiBytes = Encoding.ASCII.GetBytes(guid)
            };

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
                Logger.Error("Failed to close music client");
            }
        }

        public void SendMusicPacket(byte[] musicBytes)
        {
            try
            {
                if (!stop && (musicBytes != null))
                {
                    musicClient.IsBroadcasting = true;
                    musicClient.PacketNumber += 1;

                    // Generate packet
                    var udpVoicePacketBlufor = new UDPVoicePacket
                    {
                        GuidBytes = musicClient.GuidAsciiBytes,
                        AudioPart1Bytes = musicBytes,
                        AudioPart1Length = (ushort) musicBytes.Length,
                        Frequencies = musicClient.Frequencies.ToArray(),
                        UnitId = musicClient.UnitId,
                        Encryptions = musicClient.Encryptions.ToArray(),
                        Modulations = musicClient.Modulations.ToArray(),
                        PacketNumber = musicClient.PacketNumber
                    };

                    var encodedUdpVoicePacketBlufor = udpVoicePacketBlufor.EncodePacket();

                    // Send audio
                    audioUdpClient.Send(encodedUdpVoicePacketBlufor, encodedUdpVoicePacketBlufor.Length, serverEndPoint);
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

            byte[] message = musicClient.GuidAsciiBytes;

            // Force immediate ping once to avoid race condition before starting to listen
            audioUdpClient.Send(message, message.Length, serverEndPoint);

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
                            if (!musicClient.IsBroadcasting)
                            {
                                audioUdpClient.Send(message, message.Length, serverEndPoint);
                                Logger.Debug($"Broadcaster - pinging for music client: {musicClient.UnitId}");
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
