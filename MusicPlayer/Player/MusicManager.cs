using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using CSCore;
using CSCore.Codecs;
using CSCore.CoreAudioAPI;
using CSCore.SoundOut;
using PlaylistManager = MusicPlayer.Playlist.PlaylistManager;
using PlayData = MusicPlayer.DataStructures.PlayData;
using System.ComponentModel;

namespace MusicPlayer.Player
{
    public class MusicManager : INotifyPropertyChanged
    {
        private ISoundOut _soundOut;
        private IWaveSource _waveSource;

        PlayData lastPlay;

        DispatcherTimer playTimer;

        bool prepareNext = false;

        private static object m_lock = new object();
        private static volatile MusicManager _instance;
        public static MusicManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (m_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new MusicManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private double _length = 1.0;
        public double Length
        {
            get { return _length; }
            private set
            {
                if (_length != value)
                {
                    _length = value;
                    OnPropertyChanged("Length");
                }
            }
        }

        private double _dataRate = 100;
        public double DataRate
        {
            get { return _dataRate; }
            private set
            {
                if (_dataRate != value)
                {
                    _dataRate = value;
                    OnPropertyChanged("DataRate");
                    OnPropertyChanged("ClickJump");
                    OnPropertyChanged("KBJump");
                }
            }
        }

        public double ClickJump{ get { return DataRate * 10.0; } }
        public double KBJump { get { return DataRate * 0.5; } }

        private string _songLabel = "";
        public string SongLabel
        {
            get { return _songLabel; }
            private set
            {
                if (_songLabel != value)
                {
                    _songLabel = value;
                    OnPropertyChanged("SongLabel");
                    OnPropertyChanged("WindowTitle");
                }
            }
        }

        public string WindowTitle
        {
            get
            {
                if (SongLabel == "")
                {
                    return "Musegician";
                }

                return "Musegician: " + SongLabel;
            }
        }

        private double _volume = 1.0;
        public double Volume
        {
            get { return _volume; }
            set
            {
                if (_volume != value)
                {
                    _volume = value;
                    OnPropertyChanged("Volume");
                    if (_soundOut != null)
                    {
                        _soundOut.Volume = (float)_volume;
                    }
                }
            }
        }

        public enum PlayerState
        {
            NotLoaded = 0,
            Playing,
            Paused,
            Stopped,
            MAX
        }

        private PlayerState _state = PlayerState.NotLoaded;
        public PlayerState State
        {
            get { return _state; }
            private set
            {
                if (_state != value)
                {
                    _state = value;
                    _playerStateChanged?.Invoke(_state);
                }
            }
        }

        public delegate void StateChange(PlayerState newState);

        private static object m_eventLock = new object();
        private event StateChange _playerStateChanged;
        public event StateChange PlayerStateChanged
        {
            add
            {
                lock (m_eventLock)
                {
                    value?.Invoke(State);
                    _playerStateChanged += value;
                }
            }
            remove
            {
                lock (m_eventLock)
                {
                    _playerStateChanged -= value;
                }
            }
        }

        public delegate void TickUpdate(long position);
        public event TickUpdate tickUpdate;


        public delegate void IDNotifier(long id);
        private event IDNotifier _RecordingStarted;
        public event IDNotifier RecordingStarted
        {
            add
            {
                _RecordingStarted += value;
                if (State == PlayerState.Playing ||
                    State == PlayerState.Paused)
                {
                    value?.Invoke(lastPlay.recordingID);
                }
            }

            remove
            {
                _RecordingStarted -= value;
            }
        }

        private MusicManager()
        {
            playTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.25)
            };
            playTimer.Tick += new EventHandler(Tick_ProgressTimer);
            playTimer.Start();
        }

        public void SongFinished(object sender, PlaybackStoppedEventArgs e)
        {
            if (_soundOut != null && _soundOut.PlaybackState == PlaybackState.Stopped &&
                State != PlayerState.Stopped)
            {
                State = PlayerState.Stopped;
                prepareNext = true;
            }
        }

        public void PlaySong(PlayData playData)
        {
            lastPlay = playData;

            if (string.IsNullOrEmpty(playData.filename))
            {
                return;
            }

            SongLabel = String.Format("{0} - {1}", playData.artistName, playData.songTitle);

            CleanUp();

            if (!System.IO.File.Exists(playData.filename))
            {
                return;
            }

            _waveSource = CodecFactory.Instance.GetCodec(playData.filename)
                .ToSampleSource()
                .ToStereo()
                .ToWaveSource();

            MMDevice device = MMDeviceEnumerator.DefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            if (device == null)
            {
                return;
            }

            _soundOut = new WasapiOut()
            {
                Latency = 100,
                Device = device,
            };

            _soundOut.Initialize(_waveSource);
            _soundOut.Stopped += SongFinished;

            _soundOut.Play();
            _soundOut.Volume = (float)Volume;

            _RecordingStarted?.Invoke(playData.recordingID);

            DataRate = 0.25 * _waveSource.WaveFormat.BytesPerSecond;
            Length = _waveSource.Length;

            State = PlayerState.Playing;
        }

        private void Tick_ProgressTimer(object s, EventArgs e)
        {
            if (_soundOut != null && _waveSource != null)
            {
                switch (_soundOut.PlaybackState)
                {
                    case PlaybackState.Stopped:
                        //Do nothing
                        break;
                    case PlaybackState.Playing:
                    case PlaybackState.Paused:
                        tickUpdate?.Invoke(_waveSource.Position);
                        break;
                    default:
                        Console.WriteLine("Unexpeted playbackState: " + _soundOut.PlaybackState);
                        return;
                }
            }

            if (prepareNext)
            {
                prepareNext = false;
                PlaySong(PlaylistManager.Instance.Next());
            }
        }

        public void CleanUp()
        {
            if (_soundOut != null)
            {
                _soundOut.Dispose();
                _soundOut = null;
            }

            if (_waveSource != null)
            {
                _waveSource.Dispose();
                _waveSource = null;
            }
        }

        public void Next()
        {
            PlaySong(PlaylistManager.Instance.Next());
        }

        public void Back()
        {
            if (_soundOut != null && _waveSource != null &&
                _soundOut.PlaybackState == PlaybackState.Playing &&
                _waveSource.Position > 2.0f * _waveSource.WaveFormat.BytesPerSecond)
            {
                //Restart if it's within the first 2 seconds
                _waveSource.Position = 0;
            }
            else
            {
                PlaySong(PlaylistManager.Instance.Previous());
            }
        }

        public void Stop()
        {
            switch (State)
            {
                case PlayerState.NotLoaded:
                case PlayerState.Stopped:
                    //Do Nothing
                    break;
                case PlayerState.Playing:
                case PlayerState.Paused:
                    //Stop
                    State = PlayerState.Stopped;
                    SongLabel = "";
                    _soundOut.Stop();
                    break;
                case PlayerState.MAX:
                default:
                    Console.WriteLine("Unexpected MusicManager State: " + State);
                    break;
            }
        }

        public void Play()
        {
            switch (State)
            {
                case PlayerState.NotLoaded:
                    //Do Nothing
                    break;
                case PlayerState.Playing:
                    //Pause
                    State = PlayerState.Paused;
                    _soundOut.Pause();
                    break;
                case PlayerState.Paused:
                    //Play
                    State = PlayerState.Playing;
                    _soundOut.Play();
                    break;
                case PlayerState.Stopped:
                    //Play last
                    PlaySong(lastPlay);
                    break;
                case PlayerState.MAX:
                default:
                    Console.WriteLine("Unexpected MusicManager State: " + State);
                    break;
            }
        }

        public void DragRequest(long value)
        {
            switch (State)
            {
                case PlayerState.NotLoaded:
                case PlayerState.Stopped:
                    //Do nothing
                    break;
                case PlayerState.Playing:
                case PlayerState.Paused:
                    _waveSource.Position = value;
                    break;
                case PlayerState.MAX:
                default:
                    Console.WriteLine("Unexpected MusicManager State: " + State);
                    break;
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion // INotifyPropertyChanged
    }
}
