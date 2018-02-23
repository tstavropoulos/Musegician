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
using CSCore.Streams;
using CSCore.Streams.Effects;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using Musegician.AudioUtilities;

using PlaylistManager = Musegician.Playlist.PlaylistManager;
using PlayData = Musegician.DataStructures.PlayData;
using CSCoreEq = CSCore.Streams.Effects.Equalizer;

namespace Musegician.Player
{

    #region Enumerations

    public enum PlayerState
    {
        NotLoaded = 0,
        Playing,
        Paused,
        Stopped,
        MAX
    }

    #endregion Enumerations

    public class MusicManager : INotifyPropertyChanged, IDisposable
    {
        #region Current Stream

        private ISoundOut _soundOut;
        private IWaveSource _waveSource;
        private CSCoreEq _equalizer;
        private SpatializerStream _spatializer;

        #endregion Current Stream
        #region Audio System References

        private AudioClient _audioClient = null;
        private AudioClient AudioClient
        {
            get
            {
                if (_audioClient == null)
                {
                    _audioClient = AudioClient.FromMMDevice(Device);

                    _audioClient.Initialize(
                        shareMode: AudioClientShareMode.Shared,
                        streamFlags: AudioClientStreamFlags.None,
                        hnsBufferDuration: 1000,
                        hnsPeriodicity: 0,
                        waveFormat: _audioClient.GetMixFormat(),
                        audioSessionGuid: Guid.Empty);

                    if (_simpleAudioVolume != null)
                    {
                        _simpleAudioVolume.Dispose();
                        _simpleAudioVolume = null;
                    }
                    
                    _simpleAudioVolume = SimpleAudioVolume.FromAudioClient(_audioClient);

                    if (_audioSessionControl != null)
                    {
                        _audioSessionControl.SimpleVolumeChanged -= AudioSessionControl_SimpleVolumeChanged;
                        _audioSessionControl.Dispose();
                        _audioSessionControl = null;
                    }

                    _audioSessionControl = new AudioSessionControl(_audioClient);
                    _audioSessionControl.SimpleVolumeChanged += AudioSessionControl_SimpleVolumeChanged;

                    Volume = _simpleAudioVolume.MasterVolume;
                    Muted = _simpleAudioVolume.IsMuted;
                }

                return _audioClient;
            }
        }

        private AudioSessionControl _audioSessionControl = null;
        private AudioSessionControl AudioSessionControl
        {
            get
            {
                if (AudioClient == null || _audioSessionControl == null)
                {
                    throw new Exception("Unable to initialize AudioSessionControl");
                }

                return _audioSessionControl;
            }
        }

        private SimpleAudioVolume _simpleAudioVolume = null;
        private SimpleAudioVolume SimpleAudioVolume
        {
            get
            {
                if (AudioClient == null || _simpleAudioVolume == null)
                {
                    throw new Exception("Unable to initialize SimpleAudioVolume");
                }

                return _simpleAudioVolume;
            }
        }

        private MMDevice Device
        {
            get { return MMDeviceEnumerator.DefaultAudioEndpoint(DataFlow.Render, Role.Multimedia); }
        }

        private string _deviceIdentifier = "";
        public string DeviceIdentifier
        {
            get { return _deviceIdentifier; }
            set
            {
                if (_deviceIdentifier != value)
                {
                    _deviceIdentifier = value;

                }
            }
        }

        #endregion Audio System References
        #region Events

        public EventHandler<Equalizer.MeterUpdateArgs> MeterUpdate;

        #endregion Events
        #region Data

        PlayData lastPlay;

        DispatcherTimer playTimer;

        bool prepareNext = false;

        #endregion Data
        #region Singleton

        private static object m_lock = new object();
        private static volatile MusicManager _instance = null;
        public static MusicManager Instance
        {
            set
            {
                lock (m_lock)
                {
                    if (_instance != null)
                    {
                        throw new Exception("Tried to set non-null MusicManager instance");
                    }

                    _instance = value;
                }
            }
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

        #endregion Singleton
        #region Music Properties

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

        public double ClickJump { get { return DataRate * 10.0; } }
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
                    OnPropertyChanged("PlaybackLabel");
                }
            }
        }

        public string PlaybackLabel
        {
            get
            {
                if (SongLabel == "")
                {
                    return "";
                }

                if (TimeStamp == -1)
                {
                    return $"{SongLabel}  [{SongLengthLabel}]";
                }

                return $"{SongLabel}  [{TimestampToLabel(TimeStamp)} / {SongLengthLabel}]";
            }
        }

        private int _timeStamp = -1;
        public int TimeStamp
        {
            get { return _timeStamp; }
            set
            {
                if (_timeStamp != value)
                {
                    _timeStamp = value;
                    OnPropertyChanged("TimeStamp");
                    OnPropertyChanged("PlaybackLabel");
                }
            }
        }

        private string _songLengthLabel = "";
        public string SongLengthLabel
        {
            get { return _songLengthLabel; }
            set
            {
                if (_songLengthLabel != value)
                {
                    _songLengthLabel = value;
                    OnPropertyChanged("SongLengthLabel");
                    OnPropertyChanged("PlaybackLabel");
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

        private bool _muted = false;
        public bool Muted
        {
            get
            {
                return _muted || (_volume == 0.0);
            }
            set
            {
                if (_muted != value)
                {
                    _muted = value;
                    OnPropertyChanged("Mute");
                    SimpleAudioVolume.IsMuted = _muted;
                }
            }
        }

        public bool NonFeedbackMuted
        {
            set
            {
                if (_muted != value)
                {
                    _muted = value;
                    OnPropertyChanged("Mute");
                }
            }
        }

        private float _volume = 1.0f;
        public float Volume
        {
            get { return _volume; }
            set
            {
                value = MathExt.Clamp(value, 0.0f, 1.0f);
                if (_volume != value)
                {
                    _volume = value;
                    Muted = false;
                    OnPropertyChanged("Volume");
                    SimpleAudioVolume.MasterVolume = Volume;
                }
            }
        }

        public float NonFeedbackVolume
        {
            set
            {
                value = MathExt.Clamp(value, 0.0f, 1.0f);
                if (_volume != value)
                {
                    _volume = value;
                    OnPropertyChanged("Volume");
                }
            }
        }

        #endregion Music Properties

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
        public event TickUpdate ProgressTickUpdate;

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

        #region Constructor

        public MusicManager()
        {
            EventManager.RegisterClassHandler(
                typeof(Window),
                Keyboard.KeyDownEvent,
                new KeyEventHandler(EventKeyDown),
                false);

            EventManager.RegisterClassHandler(
                typeof(Window),
                Keyboard.KeyUpEvent,
                new KeyEventHandler(EventKeyUp),
                false);

            //Force instantiation
            if (AudioClient == null)
            {
                throw new Exception("Unable to initialize AudioClient");
            }


            playTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.25)
            };
            playTimer.Tick += new EventHandler(Tick_ProgressTimer);
            playTimer.Start();

        }

        #endregion Constructor
        #region Callbacks
        #region Callbacks Volume

        private void AudioSessionControl_SimpleVolumeChanged(
            object sender,
            AudioSessionSimpleVolumeChangedEventArgs e)
        {
            NonFeedbackMuted = e.IsMuted;
            NonFeedbackVolume = e.NewVolume;
        }

        #endregion Callbacks Volume
        #region Callbacks Audio Player

        public void SongFinished(object sender, PlaybackStoppedEventArgs e)
        {
            if (_soundOut != null &&
                _soundOut.PlaybackState == PlaybackState.Stopped &&
                State != PlayerState.Stopped)
            {
                State = PlayerState.Stopped;
                prepareNext = true;
            }
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
                        TimeStamp = (int)_waveSource.GetPosition().TotalSeconds;
                        ProgressTickUpdate?.Invoke(_waveSource.Position);
                        break;
                    default:
                        throw new Exception("Unexpeted playbackState: " + _soundOut.PlaybackState);
                }
            }

            if (prepareNext)
            {
                prepareNext = false;
                PlaySong(PlaylistManager.Instance.Next());
            }
        }

        #endregion Callbacks Audio Player
        #region Callbacks Equalizer

        public void EqualizerUpdated(object sender, Equalizer.EqualizerChangedArgs e)
        {
            //We do not care about updates if we haven't instantiated an equalizer
            if (_equalizer == null)
            {
                return;
            }

            if (e.Index == -1)
            {
                for (int i = 0; i < _equalizer.SampleFilters.Count; i++)
                {
                    _equalizer.SampleFilters[i].AverageGainDB = Equalizer.EqualizerManager.Instance.GetGain(i);
                }
            }
            else if (e.Index >= 0 && e.Index < _equalizer.SampleFilters.Count)
            {
                _equalizer.SampleFilters[e.Index].AverageGainDB = Equalizer.EqualizerManager.Instance.GetGain(e.Index);
            }
            else
            {
                throw new ArgumentException(string.Format(
                    "Bad Filter Index: {0}.  MaxValue: {1}",
                    e.Index,
                    _equalizer.SampleFilters.Count));
            }
        }

        #endregion Callbacks Equalizer
        #region Spatializer Callbacks

        public void SpatializerUpdated(object sender, Spatializer.SpatializerChangedArgs e)
        {
            //We do not care about updates if we haven't instantiated a spatializer
            if (_spatializer == null)
            {
                return;
            }

            _spatializer.PrepareNewIRFs();
        }

        #endregion Spatializer Callbacks
        #endregion Callbacks

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

            SpectralPowerStream spectralPowerStream;
            _spatializer = null;
            _equalizer = null;

            ISampleSource sampleSource = CodecFactory.Instance.GetCodec(playData.filename)
                .ToSampleSource()
                .ToStereo()
                .AppendSource(SpectralPowerStream.CreatePowerStream, out spectralPowerStream);

            if (sampleSource.WaveFormat.SampleRate >= 32_000)
            {
                sampleSource = sampleSource.AppendSource(CSCoreEq.Create10BandEqualizer, out _equalizer);
            }

            sampleSource = sampleSource.AppendSource(
                SpatializerStream.CreateSpatializerStream,
                out _spatializer);

            _waveSource = sampleSource.ToWaveSource();

            spectralPowerStream.PowerUpdate += (s, e) => MeterUpdate?.Invoke(s, e);

            if (_equalizer != null)
            {
                //Set current eq values
                for (int i = 0; i < _equalizer.SampleFilters.Count; i++)
                {
                    _equalizer.SampleFilters[i].AverageGainDB = Equalizer.EqualizerManager.Instance.GetGain(i);
                }
            }

            if (Device == null)
            {
                return;
            }

            _soundOut = new WasapiOut()
            {
                Latency = 100,
                Device = Device
            };

            _soundOut.Initialize(_waveSource);
            _soundOut.Stopped += SongFinished;

            _soundOut.Play();

            _RecordingStarted?.Invoke(playData.recordingID);

            DataRate = 0.25 * _waveSource.WaveFormat.BytesPerSecond;
            Length = _waveSource.Length;

            int duration = (int)Math.Ceiling(_waveSource.GetLength().TotalSeconds);
            SongLengthLabel = TimestampToLabel(duration);
            TimeStamp = 0;


            State = PlayerState.Playing;
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

            if (_equalizer != null)
            {
                _equalizer.Dispose();
                _equalizer = null;
            }

            if (_spatializer != null)
            {
                _spatializer.Dispose();
                _spatializer = null;
            }
        }


        #region Helper Methods

        private string TimestampToLabel(int time)
        {
            int minutes = time / 60;
            int seconds = time % 60;

            return $"{minutes.ToString()}:{seconds.ToString("D2")}";
        }

        #endregion Helper Methods
        #region Playback Control Methods

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
                TimeStamp = 0;
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
                    //Clearing the SongLabel is wrong... because Play will still start it again
                    //SongLabel = "";
                    TimeStamp = -1;
                    _soundOut.Stop();
                    break;
                case PlayerState.MAX:
                default:
                    throw new Exception("Unexpected MusicManager State: " + State);
            }
        }

        public void PlayPause()
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
                    throw new Exception("Unexpected MusicManager State: " + State);
            }
        }

        public void Pause()
        {
            switch (State)
            {
                case PlayerState.NotLoaded:
                case PlayerState.Paused:
                case PlayerState.Stopped:
                    //Do Nothing
                    break;
                case PlayerState.Playing:
                    //Pause
                    State = PlayerState.Paused;
                    _soundOut.Pause();
                    break;
                case PlayerState.MAX:
                default:
                    throw new Exception("Unexpected MusicManager State: " + State);
            }
        }

        public void MutePlayer()
        {
            Muted = true;
        }

        public void UnMutePlayer()
        {
            Muted = false;
        }

        public void MuteUnMutePlayer()
        {
            Muted = !Muted;
        }

        public void VolumeUp()
        {
            Volume += 0.1f;
        }

        public void VolumeDown()
        {
            Volume -= 0.1f;
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
                    throw new Exception("Unexpected MusicManager State: " + State);
            }
        }

        #endregion Playback Control Methods
        #region Keyboard Controls

        private enum KeyboardAction
        {
            None = 0,
            PlayFlipFlop,
            Pause,
            Stop,
            Next,
            Back,
            Mute,
            VolumeUp,
            VolumeDown,
            MAX
        }

        private KeyboardAction GetKeyAction(Key key)
        {
            switch (key)
            {
                case Key.Pause:
                    return KeyboardAction.Pause;
                case Key.Escape:
                case Key.MediaStop:
                    return KeyboardAction.Stop;
                case Key.Space:
                case Key.MediaPlayPause:
                case Key.Play:
                    return KeyboardAction.PlayFlipFlop;
                /*
                 * Disabling these - relying on the OS instead
                case Key.VolumeMute:
                    return KeyboardAction.Mute;
                case Key.VolumeDown:
                    return KeyboardAction.VolumeDown;
                case Key.VolumeUp:
                    return KeyboardAction.VolumeUp;
                */
                case Key.MediaNextTrack:
                    return KeyboardAction.Next;
                case Key.MediaPreviousTrack:
                    return KeyboardAction.Back;
                default:
                    return KeyboardAction.None;
            }
        }

        private static readonly HashSet<Key> hookKeys = new HashSet<Key>()
        {
            Key.Pause,
            Key.MediaStop,
            Key.MediaPlayPause,
            Key.Play,
            Key.MediaNextTrack,
            Key.MediaPreviousTrack
        };

        public static ICollection<Key> GetHookKeys()
        {
            return hookKeys;
        }

        public void RegisteredKeyPressed(object sender, Key key)
        {
            KeyboardAction action = GetKeyAction(key);

            if (action == KeyboardAction.None)
            {
                //No resulting action
                return;
            }

            ExecuteKeyboardAction(action);
        }

        public void EventKeyDown(object sender, KeyEventArgs e)
        {
            if (hookKeys.Contains(e.Key))
            {
                //Skip handling the hooked keys, they'll come from the OS
                return;
            }

            KeyboardAction action = GetKeyAction(e.Key);

            if (action == KeyboardAction.None)
            {
                //No resulting action
                return;
            }

            e.Handled = true;
            ExecuteKeyboardAction(action);
        }

        public void EventKeyUp(object sender, KeyEventArgs e)
        {
            if (hookKeys.Contains(e.Key))
            {
                //Skip handling the hooked keys, they'll come from the OS
                return;
            }

            KeyboardAction action = GetKeyAction(e.Key);

            if (action == KeyboardAction.None)
            {
                //No resulting action
                return;
            }

            e.Handled = true;
        }

        private void ExecuteKeyboardAction(KeyboardAction action)
        {
            switch (action)
            {
                case KeyboardAction.None:
                    //Do nothing
                    return;
                case KeyboardAction.PlayFlipFlop:
                    PlayPause();
                    break;
                case KeyboardAction.Pause:
                    Pause();
                    break;
                case KeyboardAction.Stop:
                    Stop();
                    break;
                case KeyboardAction.Next:
                    Next();
                    break;
                case KeyboardAction.Back:
                    Back();
                    break;
                case KeyboardAction.Mute:
                    MuteUnMutePlayer();
                    break;
                case KeyboardAction.VolumeUp:
                    VolumeUp();
                    break;
                case KeyboardAction.VolumeDown:
                    VolumeDown();
                    break;
                case KeyboardAction.MAX:
                default:
                    throw new Exception("Unexpected KeyboardAction: " + action);
            }
        }

        #endregion Keyboard Controls
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
        #region IDisposable

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_simpleAudioVolume != null)
                    {
                        _simpleAudioVolume.Dispose();
                        _simpleAudioVolume = null;
                    }

                    if (_audioSessionControl != null)
                    {
                        _audioSessionControl.SimpleVolumeChanged -= AudioSessionControl_SimpleVolumeChanged;
                        _audioSessionControl.Dispose();
                        _audioSessionControl = null;
                    }

                    if (_audioClient == null)
                    {
                        _audioClient.Dispose();
                        _audioClient = null;
                    }

                    if (_soundOut != null)
                    {
                        _soundOut.Dispose();
                        _soundOut = null;
                    }

                    if (_equalizer != null)
                    {
                        _equalizer.Dispose();
                        _equalizer = null;
                    }

                    if (_spatializer != null)
                    {
                        _spatializer.Dispose();
                        _spatializer = null;
                    }

                    if (_waveSource != null)
                    {
                        _waveSource.Dispose();
                        _waveSource = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion IDisposable
    }
}
