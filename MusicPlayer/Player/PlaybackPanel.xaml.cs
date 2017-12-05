using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Threading;
using CSCore;
using CSCore.Codecs;
using CSCore.CoreAudioAPI;
using CSCore.SoundOut;

namespace MusicPlayer.Player
{
    /// <summary>
    /// Interaction logic for PlaybackPanel.xaml
    /// </summary>
    public partial class PlaybackPanel : UserControl
    {
        private ISoundOut _soundOut;
        private IWaveSource _waveSource;

        DataStructures.PlayData lastPlay;

        DispatcherTimer playTimer;

        bool prepareNext = false;

        const string playString = "▶";
        const string pauseString = "||";

        private int reentryBlock = 0;

        public event EventHandler<PlaybackStoppedEventArgs> PlaybackStopped;

        private double _volume = 1.0;
        public double Volume
        {
            get { return _volume; }
            set
            {
                if (_volume != value)
                {
                    _volume = value;
                    if (_soundOut != null)
                    {
                        _soundOut.Volume = (float)_volume;
                    }
                }
            }
        }

        public PlaybackPanel()
        {
            InitializeComponent();

            PlaybackStopped += SongFinished;

            playButton.Content = playString;
            playButton.Foreground = new SolidColorBrush(Colors.Black);
            stopButton.Foreground = new SolidColorBrush(Colors.Black);

            playTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.25)
            };
            playTimer.Tick += new EventHandler(Tick_ProgressTimer);
            playTimer.Start();

            progressSlider.Minimum = 0.0;
        }

        public delegate void SimpleEvent();

        public event SimpleEvent NextClicked;
        public event SimpleEvent BackClicked;
        public event SimpleEvent PlaybackFinished;

        public void SongFinished(object sender, PlaybackStoppedEventArgs e)
        {
            if (reentryBlock == 0)
            {
                ++reentryBlock;
                Dispatcher.Invoke(() =>
                {
                    playButton.Content = playString;
                    playButton.Foreground = new SolidColorBrush(Colors.LightGreen);
                    stopButton.Foreground = new SolidColorBrush(Colors.Black);
                    prepareNext = true;
                });
                --reentryBlock;
            }
        }

        public void CleanUp()
        {
            if (_soundOut == null)
            {
                return;
            }

            switch (_soundOut.PlaybackState)
            {
                case PlaybackState.Stopped:
                    //Do nothing
                    break;
                case PlaybackState.Playing:
                case PlaybackState.Paused:
                    ManualStop();
                    break;
                default:
                    ManualStop();
                    Console.WriteLine("Unexpeted playbackState: " + _soundOut.PlaybackState);
                    return;
            }
        }

        public void PlaySong(DataStructures.PlayData playData)
        {
            lastPlay = playData;
            if (_soundOut != null)
            {
                switch (_soundOut.PlaybackState)
                {
                    case PlaybackState.Stopped:
                        //Do nothing
                        break;
                    case PlaybackState.Playing:
                    case PlaybackState.Paused:
                        ManualStop();
                        break;
                    default:
                        ManualStop();
                        Console.WriteLine("Unexpeted playbackState: " + _soundOut.PlaybackState);
                        return;
                }
            }

            if (string.IsNullOrEmpty(playData.filename))
            {
                return;
            }

            songLabel.Content = String.Format("{0} - {1}", playData.artistName, playData.songTitle);

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

            if (PlaybackStopped != null)
            {
                _soundOut.Stopped += PlaybackStopped;
            }

            _soundOut.Play();
            _soundOut.Volume = (float)Volume;

            stopButton.Foreground = new SolidColorBrush(Colors.Red);
            playButton.Foreground = new SolidColorBrush(Colors.LightGreen);
            playButton.Content = pauseString;

            progressSlider.TickFrequency = 0.25 * _waveSource.WaveFormat.BytesPerSecond;
            progressSlider.Maximum = _waveSource.Length;
        }

        public void OnPlayClick(object sender, RoutedEventArgs e)
        {
            if (_soundOut == null)
            {
                return;
            }

            switch (_soundOut.PlaybackState)
            {
                case PlaybackState.Stopped:
                    PlaySong(lastPlay);
                    break;
                case PlaybackState.Playing:
                    _soundOut.Pause();
                    playButton.Content = playString;
                    break;
                case PlaybackState.Paused:
                    _soundOut.Play();
                    playButton.Content = pauseString;
                    break;
                default:
                    ManualStop();
                    Console.WriteLine("Unexpeted playbackState: " + _soundOut.PlaybackState);
                    return;
            }
        }

        public void OnStopClick(object sender, RoutedEventArgs e)
        {
            if (_soundOut == null)
            {
                return;
            }

            switch (_soundOut.PlaybackState)
            {
                case PlaybackState.Stopped:
                    //Do nothing
                    break;
                case PlaybackState.Playing:
                case PlaybackState.Paused:
                    ManualStop();
                    playButton.Content = playString;
                    break;
                default:
                    ManualStop();
                    Console.WriteLine("Unexpeted playbackState: " + _soundOut.PlaybackState);
                    return;
            }

            stopButton.Foreground = new SolidColorBrush(Colors.Black);
        }

        public void OnNextClick(object sender, RoutedEventArgs e)
        {
            NextClicked?.Invoke();
        }

        public void OnBackClick(object sender, RoutedEventArgs e)
        {
            if (_soundOut != null && _waveSource != null &&
                _soundOut.PlaybackState == PlaybackState.Playing &&
                _waveSource.Position > 2.0f * _waveSource.WaveFormat.BytesPerSecond)
            {
                //Restart if it's within the first 3 seconds
                _waveSource.Position = 0;
            }
            else
            {
                BackClicked?.Invoke();
            }
        }

        private void ManualStop()
        {
            ++reentryBlock;
            _soundOut.Stop();
            --reentryBlock;
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
                        progressSlider.Value = _waveSource.Position;
                        break;
                    default:
                        Console.WriteLine("Unexpeted playbackState: " + _soundOut.PlaybackState);
                        return;
                }
            }

            if (prepareNext)
            {
                prepareNext = false;
                PlaybackFinished?.Invoke();
            }
        }

        private void Slider_Drag(object s, DragDeltaEventArgs e)
        {
            if (_soundOut == null || _waveSource == null)
            {
                return;
            }

            switch (_soundOut.PlaybackState)
            {
                case PlaybackState.Stopped:
                    //Do nothing
                    break;
                case PlaybackState.Playing:
                case PlaybackState.Paused:
                    _waveSource.Position = (long)progressSlider.Value;
                    break;
                default:
                    Console.WriteLine("Unexpeted playbackState: " + _soundOut.PlaybackState);
                    return;
            }
        }
    }
}
