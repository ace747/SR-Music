using FragLabs.Audio.Codecs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using Application = FragLabs.Audio.Codecs.Opus.Application;

namespace DCS_SR_Music.Network
{
    public class MusicController
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private int stationNumber;
        private string directory;
        private Dictionary<string, string> trackDictionary = new Dictionary<string, string>();
        private List<string> audioTracksList;
        private bool playMusic;
        private int trackIndex;
        private bool skip = false;
        private bool repeat = false;
        private object trackLock = new object();
        private TimeSpan trackTime = TimeSpan.Zero;
        private readonly TimeSpan threeSeconds = new TimeSpan(0, 0, 3);

        private int INPUT_AUDIO_LENGTH_MS = 40;
        private int INPUT_SAMPLE_RATE = 48000;
        private int SEGMENT_FRAMES;
        private WaveFormat format;
        private OpusEncoder encoder;
        private readonly Queue<(byte[], TimeSpan)> audioSampleQueue = new Queue<(byte[], TimeSpan)>();

        // Events
        public event Action<byte[]> Broadcast;
        public event Action StopMusic;
        public event Action<int, string> TrackNameUpdate;
        public event Action<int, string> TrackTimerUpdate;
        public event Action<int, string> TrackNumberUpdate;

        public MusicController(int statNumber)
        {
            stationNumber = statNumber;

            SEGMENT_FRAMES = (INPUT_SAMPLE_RATE / 1000) * INPUT_AUDIO_LENGTH_MS;
            format = new WaveFormat(INPUT_SAMPLE_RATE, 16, 1);
            encoder = OpusEncoder.Create(INPUT_SAMPLE_RATE, 1, Application.Audio);
            encoder.ForwardErrorCorrection = false;
        }

        public void SetDirectory(string dir)
        {
            Logger.Debug($"Music directory set to path {dir} on station {stationNumber}");
            directory = dir;
        }

        public void Start()
        {
            Logger.Info($"Starting music playblack on station {stationNumber}");

            trackDictionary.Clear();
            int numTracks = ReadTracks();

            if (numTracks == 0)
            {
                // No playable tracks found
                ShowAudioTrackWarning();
                return;
            }

            audioTracksList = trackDictionary.Keys.ToList();
            ShuffleAudioTracksList();

            skip = false;
            playMusic = true;
            Play();
        }

        public void Stop()
        {
            Logger.Info($"Stopping music playblack on station {stationNumber}");
            playMusic = false;
        }

        public void Play()
        {
            System.Timers.Timer trackTimer = new System.Timers.Timer();
            trackTimer.Elapsed += new ElapsedEventHandler(TrackTimerTicked);
            trackTimer.Interval = INPUT_AUDIO_LENGTH_MS;
            trackTimer.Enabled = true;

            while (playMusic)
            {
                string trackPath = "";
                string trackName = "";

                try
                {
                    // Sleep for 100ms between tracks to minimize clipping
                    Thread.Sleep(100);

                    trackPath = GetNextTrack();
                    trackName = trackDictionary[trackPath];

                    Logger.Info($"Station {stationNumber} is now playing: {trackName}");

                    if (trackName.Length > 30)
                    {
                        trackName = trackName.Substring(0, 30) + "...";
                    }

                    trackTime = TimeSpan.Zero;
                    TrackNameUpdate(stationNumber, trackName);
                    TrackNumberUpdate(stationNumber, GetTrackNumLabel());

                    audioSampleQueue.Clear();
                    Task writeTask = Task.Run(() => WriteAudioSampleQueue(trackPath));
                    Task readTask = Task.Run(() => ReadAudioSampleQueue());

                    Task.WaitAll(writeTask, readTask);

                    lock (trackLock)
                    {
                        if (skip)
                        {
                            // Don't skip to previous if more than 3 seconds into song
                            if (trackTime >= threeSeconds && audioTracksList.IndexOf(trackPath) > trackIndex)
                            {
                                trackIndex = audioTracksList.IndexOf(trackPath);
                            }
                        }

                        else if (!repeat)
                        {
                            trackIndex += 1;
                        }
                    }
                }

                catch (Exception ex)
                {
                    trackDictionary.Remove(trackPath);

                    if (trackDictionary.Count > 1)
                    {
                        Logger.Error(ex, $"Error encountered during audio playback of track: {trackPath}.  Error: ");
                        audioTracksList.RemoveAt(trackIndex);
                        ShowAudioTrackError(trackName);
                        continue;
                    }

                    else
                    {
                        Logger.Error(ex, $"Error encountered during audio playback on station: {stationNumber}.  Error: ");
                        StopMusic();
                    }
                }
            }
        }

        public void SkipForward()
        {
            lock(trackLock)
            {
                if (playMusic)
                {
                    Logger.Info($"Music Controller skip forward on station {stationNumber}");
                    skip = true;
                    trackIndex += 1;
                }
            }
        }

        public void SkipBackward()
        {
            lock (trackLock)
            {
                if (playMusic)
                {
                    Logger.Info($"Music Controller skip backward on station {stationNumber}");
                    skip = true;
                    trackIndex -= 1;
                }
            }
        }

        public void Repeat(bool value)
        {
            lock (trackLock)
            {
                Logger.Info($"Music Controller repeat enabled on station {stationNumber}");
                repeat = value;
            }
        }

        private void WriteAudioSampleQueue(string trackPath)
        {
            try
            {
                byte[] buffer = new byte[SEGMENT_FRAMES * 2];

                using (Mp3FileReader track = new Mp3FileReader(trackPath))
                {
                    using (var conversionStream = new WaveFormatConversionStream(format, track))
                    {
                        using (WaveStream pcm = WaveFormatConversionStream.CreatePcmStream(conversionStream))
                        {
                            while (playMusic && !skip && (pcm.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                // Encode as opus bytes
                                int len;
                                var buff = encoder.Encode(buffer, buffer.Length, out len);

                                // Create copy with small buffer
                                var encoded = new byte[len];
                                Buffer.BlockCopy(buff, 0, encoded, 0, len);

                                audioSampleQueue.Enqueue((encoded, pcm.CurrentTime));

                                Array.Clear(buffer, 0, buffer.Length);
                            }
                        }
                    }
                }
                
                // Write null audioBytes to signal end of track
                audioSampleQueue.Enqueue((null, TimeSpan.Zero));
            }

            catch (Exception ex)
            {
                Logger.Error($"Music Controller encountered exception when writing to AudioSampleQueue on station {stationNumber}:  {ex}");
            }
        }

        private void ReadAudioSampleQueue()
        {
            bool endOfTrack = false;

            Stopwatch watch = new Stopwatch();
            watch.Start();

            while (playMusic && !skip)
            {
                if (endOfTrack)
                {
                    break;
                }

                if (audioSampleQueue.Count > 0)
                {
                    var audioSample = audioSampleQueue.Dequeue();
                    var audioBytes = audioSample.Item1;

                    if (audioBytes == null)
                    {
                        endOfTrack = true;
                        continue;
                    }

                    trackTime = audioSample.Item2;

                    // Broadcast audio bytes to SRS clients
                    Broadcast(audioBytes);

                    int timeDiff = (int)(watch.ElapsedMilliseconds - trackTime.TotalMilliseconds);
                    int sleepTime = INPUT_AUDIO_LENGTH_MS - timeDiff;

                    if (sleepTime > 0)
                    {
                         Thread.Sleep(sleepTime);
                    }
                }
            }
        }

        private void TrackTimerTicked(object source, ElapsedEventArgs e)
        {
            if (playMusic)
            {
                string time = String.Format("{0:00}:{1:00}", trackTime.Minutes, trackTime.Seconds);
                TrackTimerUpdate(stationNumber, time);
            }
        }

        private int ReadTracks()
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    string[] files = Directory.GetFiles(directory, "*.mp3", SearchOption.TopDirectoryOnly);

                    foreach (string f in files)
                    {
                        string filename = Path.GetFileNameWithoutExtension(f);

                        trackDictionary.Add(f, filename);
                    }

                    return trackDictionary.Count;
                }

                else
                {
                    return 0;
                }
            }

            catch (IOException ex)
            {
                Logger.Warn(ex, "Unable to read tracks due to error: ");
                return 0;
            }
        }

        private void ShuffleAudioTracksList()
        {
            var rand = new Random();
            List<string> shuffledList;

            // At end of playlist/queue - shuffle and guarantee no repeat
            if (audioTracksList.Count > 1)
            {
                string lastSong = audioTracksList[audioTracksList.Count - 1];
                audioTracksList = trackDictionary.Keys.ToList();
                shuffledList = audioTracksList.OrderBy(x => rand.Next()).ToList();

                while (shuffledList[0] == lastSong)
                {
                    shuffledList = audioTracksList.OrderBy(x => rand.Next()).ToList();
                }

                audioTracksList = shuffledList;

                lock (trackLock)
                {
                    trackIndex = 0;
                }
            }
        }

        private string GetNextTrack()
        {
            try
            {
                if (trackIndex >= audioTracksList.Count)
                {
                    if (audioTracksList.Count > 1)
                    {
                        ShuffleAudioTracksList();
                    }

                    else if (audioTracksList.Count == 1)
                    {
                        lock (trackLock)
                        {
                            trackIndex = 0;
                        }
                    }
                }

                lock (trackLock)
                {
                    if (skip)
                    {
                        skip = false;

                        if (trackIndex < 0)
                        {
                            trackIndex = 0;
                        }
                    }
                }

                return audioTracksList[trackIndex];
            }

            catch (Exception)
            {
                Logger.Warn($"Music Controller on station {stationNumber} unable to get next track.");
                return "";
            }
        }

        private string GetTrackNumLabel()
        {
            int num = trackIndex + 1;

            return num.ToString() + " of " + audioTracksList.Count;
        }

        private void ShowAudioTrackWarning()
        {
            Logger.Warn("Audio track warning! No valid audio tracks were found.");

            StopMusic();

            MessageBox.Show($"No playable Audio Tracks were found. " +
                            $"\n\nPlease ensure tracks are of MP3 format, and placed in the selected directory.",
                            "No Tracks Found",
                            MessageBoxButton.OK,
                            MessageBoxImage.Exclamation);
        }

        private void ShowAudioTrackError(string trackName)
        {
            MessageBox.Show($"Error during track playback." +
                            $"\n\nTrack: {trackName}",
                            "Track Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
        }
    }
}
