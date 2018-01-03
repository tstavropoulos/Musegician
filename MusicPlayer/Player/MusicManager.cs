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
using PlaylistManager = MusicPlayer.Playlist.PlaylistManager;
using PlayData = MusicPlayer.DataStructures.PlayData;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;

namespace MusicPlayer.Player
{
    public class MusicManager : INotifyPropertyChanged, IDisposable
    {
        private ISoundOut _soundOut;
        private IWaveSource _waveSource;
        private CSCore.Streams.Effects.Equalizer _equalizer;

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


        PlayData lastPlay;

        DispatcherTimer playTimer;

        bool prepareNext = false;

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

        private bool suppressUpdate = false;

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

                    suppressUpdate = true;
                    SimpleAudioVolume.IsMuted = _muted;
                }
            }
        }

        private float _volume = 1.0f;
        public float Volume
        {
            get
            {
                return _volume;
            }
            set
            {
                value = MathExt.Clamp(value, 0.0f, 1.0f);
                if (_volume != value)
                {
                    _volume = value;
                    Muted = false;

                    OnPropertyChanged("Volume");

                    //Update the volume
                    if (_soundOut != null)
                    {
                        _soundOut.Volume = Volume;
                    }

                    suppressUpdate = true;
                    SimpleAudioVolume.MasterVolume = Volume;
                }
            }
        }

        private void AudioSessionControl_SimpleVolumeChanged(
            object sender,
            AudioSessionSimpleVolumeChangedEventArgs e)
        {
            if (!suppressUpdate)
            {
                Muted = e.IsMuted;
                Volume = e.NewVolume;
            }

            suppressUpdate = false;
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

            Equalizer.EqualizerManager.Instance.PropertyChanged += EqualizerUpdated;

            playTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.25)
            };
            playTimer.Tick += new EventHandler(Tick_ProgressTimer);
            playTimer.Start();

        }

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
                .AppendSource(CSCore.Streams.Effects.Equalizer.Create10BandEqualizer, out _equalizer)
                .ToWaveSource();

            //Set current eq values
            for (int i = 0; i < _equalizer.SampleFilters.Count; i++)
            {
                _equalizer.SampleFilters[i].AverageGainDB = Equalizer.EqualizerManager.Instance.GetGain(i);
            }

            MMDevice device = MMDeviceEnumerator.DefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            if (device == null)
            {
                return;
            }

            AudioClient temp = AudioClient.FromMMDevice(Device);

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
                        throw new Exception("Unexpeted playbackState: " + _soundOut.PlaybackState);
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

            if (_equalizer != null)
            {
                _equalizer.Dispose();
                _equalizer = null;
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

        private void EqualizerUpdated(object sender, PropertyChangedEventArgs e)
        {
            //We do not care about updates if we haven't instantiated an equalizer
            if (_equalizer == null)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case "EqCh0":
                    _equalizer.SampleFilters[0].AverageGainDB = Equalizer.EqualizerManager.Instance.EqCh0;
                    break;
                case "EqCh1":
                    _equalizer.SampleFilters[1].AverageGainDB = Equalizer.EqualizerManager.Instance.EqCh1;
                    break;
                case "EqCh2":
                    _equalizer.SampleFilters[2].AverageGainDB = Equalizer.EqualizerManager.Instance.EqCh2;
                    break;
                case "EqCh3":
                    _equalizer.SampleFilters[3].AverageGainDB = Equalizer.EqualizerManager.Instance.EqCh3;
                    break;
                case "EqCh4":
                    _equalizer.SampleFilters[4].AverageGainDB = Equalizer.EqualizerManager.Instance.EqCh4;
                    break;
                case "EqCh5":
                    _equalizer.SampleFilters[5].AverageGainDB = Equalizer.EqualizerManager.Instance.EqCh5;
                    break;
                case "EqCh6":
                    _equalizer.SampleFilters[6].AverageGainDB = Equalizer.EqualizerManager.Instance.EqCh6;
                    break;
                case "EqCh7":
                    _equalizer.SampleFilters[7].AverageGainDB = Equalizer.EqualizerManager.Instance.EqCh7;
                    break;
                case "EqCh8":
                    _equalizer.SampleFilters[8].AverageGainDB = Equalizer.EqualizerManager.Instance.EqCh8;
                    break;
                case "EqCh9":
                    _equalizer.SampleFilters[9].AverageGainDB = Equalizer.EqualizerManager.Instance.EqCh9;
                    break;
                default:
                    //Do nothing
                    return;
            }
        }

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
