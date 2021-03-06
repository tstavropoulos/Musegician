﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Input;
using CSCore;
using CSCore.Codecs;
using CSCore.CoreAudioAPI;
using CSCore.SoundOut;
using Musegician.AudioUtilities;
using Musegician.Database;

using PlaylistManager = Musegician.Playlist.PlaylistManager;
using PlayData = Musegician.DataStructures.PlayData;
using CSCoreEq = CSCore.Streams.Effects.Equalizer;
using ILooperUpdater = Musegician.Driller.ILooperUpdater;

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
        private PhaseVocoderStream _phaseStream;

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

        private MMDevice Device => MMDeviceEnumerator.DefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

        private string _deviceIdentifier = "";
        public string DeviceIdentifier
        {
            get => _deviceIdentifier;
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
        #region LooperUpdaters

        private ILooperUpdater AttachedLooper
        {
            get
            {
                if (_looperUpdater != null && _looperUpdater.TryGetTarget(out ILooperUpdater updater))
                {
                    return updater;
                }

                _looperUpdater = null;
                return null;
            }
        }

        private WeakReference<ILooperUpdater> _looperUpdater = null;

        public void SetLooperUpdater(ILooperUpdater listener)
        {
            _looperUpdater = new WeakReference<ILooperUpdater>(listener);
            listener.ResetBounds();
        }

        public void RemoveLooperUpdater(ILooperUpdater updater)
        {
            _looperUpdater = null;
            EndPosition = Length;
        }

        #endregion LooperUpdaters
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
            get => _length;
            private set
            {
                if (_length != value)
                {
                    _length = value;
                    EndPosition = value;

                    SongLengthLabel = TimestampToLabel((int)Math.Floor(Length));

                    OnPropertyChanged("Length");
                    OnPropertyChanged("SongLengthLabel");
                    OnPropertyChanged("PlaybackLabel");
                }
            }
        }

        private double _endPosition = 1.0;
        public double EndPosition
        {
            get => _endPosition;
            set
            {
                if (_endPosition != value)
                {
                    _endPosition = value;

                    OnPropertyChanged("EndPosition");
                }
            }
        }

        public double StartPosition
        {
            set
            {
                if (Position < value)
                {
                    Position = value;
                }
            }
        }

        public double ClickJump => 10.0;
        public double KBJump => 0.5;

        private string _songLabel = "";
        public string SongLabel
        {
            get => _songLabel;
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

                if (PositionSeconds == -1)
                {
                    return $"{SongLabel}  [{SongLengthLabel}]";
                }

                return $"{SongLabel}  [{TimestampToLabel(PositionSeconds)} / {SongLengthLabel}]";
            }
        }
        public string SongLengthLabel { get; private set; } = "";

        public string WindowTitle
        {
            get
            {
                if (SongLabel == "")
                {
                    return "Musegician";
                }

                return $"Musegician: {SongLabel}";
            }
        }

        private bool _muted = false;
        public bool Muted
        {
            get => _muted || (_volume == 0.0);
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
            get => _volume;
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

        private double _position = 0.0;
        public double Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    SetPositionRequest(_position);
                    OnPropertyChanged("Position");
                    PositionSeconds = (int)Math.Floor(_position);
                }
            }
        }

        public double NonFeedbackPosition
        {
            set
            {
                if (_position != value)
                {
                    _position = value;
                    OnPropertyChanged("Position");
                    PositionSeconds = (int)Math.Floor(_position);
                }
            }
        }

        private int _positionSeconds = 0;
        private int PositionSeconds
        {
            get => _positionSeconds;
            set
            {
                if (_positionSeconds != value)
                {
                    _positionSeconds = value;
                    OnPropertyChanged("PlaybackLabel");
                }
            }
        }

        #endregion Music Properties

        private PlayerState _state = PlayerState.NotLoaded;
        public PlayerState State
        {
            get => _state;
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

        public delegate void RecordingNotifier(Recording recording);
        private event RecordingNotifier _RecordingStarted;
        public event RecordingNotifier RecordingStarted
        {
            add
            {
                _RecordingStarted += value;
                if (State == PlayerState.Playing ||
                    State == PlayerState.Paused)
                {
                    value?.Invoke(lastPlay.recording);
                }
            }

            remove => _RecordingStarted -= value;
        }

        private float speed = 1.0f;
        public float Speed
        {
            get => speed;
            set
            {
                if (speed != value)
                {
                    speed = value;

                    if (_phaseStream != null)
                    {
                        _phaseStream.Speed = value;
                        _phaseStream.Enabled = speed < 1f;
                    }

                    OnPropertyChanged("Speed");
                }
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
            ILooperUpdater looper = AttachedLooper;
            if (_soundOut != null && _waveSource != null)
            {
                switch (_soundOut.PlaybackState)
                {
                    case PlaybackState.Stopped:
                        //Do nothing
                        break;
                    case PlaybackState.Playing:
                    case PlaybackState.Paused:
                        NonFeedbackPosition = _waveSource.GetPosition().TotalSeconds;
                        if (looper != null && Position >= _endPosition)
                        {
                            prepareNext = true;
                        }
                        break;
                    default:
                        throw new Exception($"Unexpeted playbackState: {_soundOut.PlaybackState}");
                }
            }

            if (prepareNext)
            {
                prepareNext = false;

                if (looper != null)
                {
                    Restart();
                }
                else
                {
                    PlaySong(PlaylistManager.Instance.Next());
                }
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
                throw new ArgumentException(
                    $"Bad Filter Index: {e.Index}.  MaxValue: {_equalizer.SampleFilters.Count}");
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

        public void PlaySong(PlayData playData, bool preserveLoopBounds = false)
        {
            if (playData.recording is null ||
                string.IsNullOrEmpty(playData.recording.Filename))
            {
                return;
            }

            lastPlay = playData;

            SongLabel = $"{playData.artistName} - {playData.songTitle}";

            CleanUp();

            if (!System.IO.File.Exists(playData.recording.Filename))
            {
                return;
            }

            _spatializer = null;
            _equalizer = null;
            _phaseStream = null;

            ISampleSource sampleSource = CodecFactory.Instance.GetCodec(playData.recording.Filename)
                .ToSampleSource()
                .ToStereo();

            SpectralPowerStream spectralPowerStream = null;

            if (sampleSource.WaveFormat.SampleRate >= 16_000)
            {
                sampleSource = sampleSource.AppendSource(SpectralPowerStream.CreatePowerStream, out spectralPowerStream);
            }

            if (sampleSource.WaveFormat.SampleRate >= 32_000)
            {
                sampleSource = sampleSource.AppendSource(CSCoreEq.Create10BandEqualizer, out _equalizer);
            }

            if (sampleSource.WaveFormat.SampleRate >= 16_000)
            {
                sampleSource = sampleSource.AppendSource(
                    SpatializerStream.CreateSpatializerStream,
                    out _spatializer);



                sampleSource = sampleSource.AppendSource(
                    PhaseVocoderStream.CreatePhaseVocodedStream,
                    out _phaseStream);

                _phaseStream.Speed = Speed;
                _phaseStream.Enabled = Speed != 1.0f;
            }

            _waveSource = sampleSource.ToWaveSource();

            if (spectralPowerStream != null)
            {
                spectralPowerStream.PowerUpdate += (s, e) => MeterUpdate?.Invoke(s, e);
            }


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

            _RecordingStarted?.Invoke(playData.recording);

            Length = _waveSource.GetLength().TotalSeconds;
            NonFeedbackPosition = 0.0;

            ILooperUpdater looper = AttachedLooper;
            if (looper != null)
            {
                if (!preserveLoopBounds)
                {
                    looper.ResetBounds();
                }

                Position = looper.GetStartPosition();
            }

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

            if (_phaseStream != null)
            {
                _phaseStream.Dispose();
                _phaseStream = null;
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

        public void Next() => PlaySong(PlaylistManager.Instance.Next());

        public void Back()
        {
            if (_soundOut != null && _waveSource != null &&
                (_soundOut.PlaybackState == PlaybackState.Playing || _soundOut.PlaybackState == PlaybackState.Paused) &&
                Position > 2.0 &&
                AttachedLooper == null)
            {
                //Restart if it's not within the first 2 seconds
                Position = 0.0;
            }
            else
            {
                PlaySong(PlaylistManager.Instance.Previous());
            }
        }

        public void Restart()
        {
            if (_soundOut != null && _waveSource != null)
            {
                ILooperUpdater looper = AttachedLooper;

                if (looper == null)
                {
                    Position = 0.0;
                }
                else
                {
                    Position = looper.GetStartPosition();
                    _endPosition = looper.GetEndPosition();
                }

                if (_soundOut.PlaybackState != PlaybackState.Playing)
                {
                    State = PlayerState.Playing;
                    _soundOut.Play();
                }
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
                    NonFeedbackPosition = -1.0;
                    _soundOut.Stop();
                    break;

                case PlayerState.MAX:
                default:
                    throw new Exception($"Unexpected MusicManager State: {State}");
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
                    throw new Exception($"Unexpected MusicManager State: {State}");
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
                    throw new Exception($"Unexpected MusicManager State: {State}");
            }
        }

        public void MutePlayer() => Muted = true;
        public void UnMutePlayer() => Muted = false;
        public void MuteUnMutePlayer() => Muted = !Muted;

        public void VolumeUp() => Volume += 0.1f;
        public void VolumeDown() => Volume -= 0.1f;

        public void SetPositionRequest(double time)
        {
            switch (State)
            {
                case PlayerState.NotLoaded:
                    //Do nothing
                    break;

                case PlayerState.Stopped:
                case PlayerState.Playing:
                case PlayerState.Paused:
                    _waveSource.SetPosition(TimeSpan.FromSeconds(time));
                    break;

                case PlayerState.MAX:
                default:
                    throw new Exception($"Unexpected MusicManager State: {State}");
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
                case Key.Pause: return KeyboardAction.Pause;
                case Key.Escape:
                case Key.MediaStop: return KeyboardAction.Stop;
                case Key.Space:
                case Key.MediaPlayPause:
                case Key.Play: return KeyboardAction.PlayFlipFlop;
                case Key.MediaNextTrack: return KeyboardAction.Next;
                case Key.MediaPreviousTrack: return KeyboardAction.Back;
                default: return KeyboardAction.None;
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

            System.Diagnostics.Debug.WriteLine($"System Press: {key}");
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
            System.Diagnostics.Debug.WriteLine($"App Press: {action}");
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
                    throw new Exception($"Unexpected KeyboardAction: {action}");
            }
        }

        #endregion Keyboard Controls
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion INotifyPropertyChanged
        #region IDisposable

        private bool disposedValue = false; // To detect redundant calls

        ~MusicManager()
        {
            CleanUpResources();
        }

        private void CleanUpResources()
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

            if (_audioClient != null)
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

            if (_phaseStream != null)
            {
                _phaseStream.Dispose();
                _phaseStream = null;
            }

            if (_waveSource != null)
            {
                _waveSource.Dispose();
                _waveSource = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    CleanUpResources();
                }

                // free unmanaged resources (unmanaged objects) and override a finalizer below.
                // set large fields to null.

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable
    }
}
