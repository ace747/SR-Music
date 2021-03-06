﻿using NLog;
using System;
using System.Net;
using System.Threading;

namespace DCS_SR_Music.Network
{
    public class Station
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public MusicController StationMusicController { get; }
        public Broadcaster StationBroadcaster { get; }
        public int StationNumber { get; }
        public string StationName { get; set; }
        public string Directory { get; set; }
        public System.Double Frequency { get; set; }
        public int Modulation { get; set; } = 0;
        public bool PlayingMusic { get; set; } = false;

        // Events
        public event Action<int, System.Double, int> UpdateStationRadio;
        public event Action<int> StopMusicEvent;

        public Station(int num, string directory, IPEndPoint endPoint, string clientGuid)
        {
            StationNumber = num;
            Directory = new Uri(directory).LocalPath;

            StationMusicController = new MusicController(StationNumber);
            StationMusicController.StopMusic += StopMusic;
            StationMusicController.Broadcast += Broadcast;

            StationBroadcaster = new Broadcaster(StationNumber, endPoint, clientGuid);
        }

        private void Broadcast(byte[] audioBytes)
        {
            StationBroadcaster.SendMusicPacket(audioBytes);
        }

        public void StartMusic()
        {
            UpdateStationRadio(StationNumber, Frequency * 1000000, Modulation);
            StationMusicController.SetDirectory(Directory);
            PlayingMusic = true;
            new Thread(StationMusicController.Start).Start();
        }

        public void StopMusic()
        {
            try
            {
                StationMusicController.Stop();
                PlayingMusic = false;
                StopMusicEvent(StationNumber);
            }

            catch (Exception ex)
            {
                Logger.Error(ex, "Exception encountered when stopping music");
            }            
        }
    }
}
