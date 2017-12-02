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

namespace MusicPlayer.Player
{
    /// <summary>
    /// Interaction logic for PlaybackPanel.xaml
    /// </summary>
    public partial class PlaybackPanel : UserControl
    {
        CSCore.SoundOut.WaveOut waveOut = null;

        DataStructures.PlayData lastPlay;

        DispatcherTimer playTimer;

        bool prepareNext = false;

        string playString;
        string pauseString;

        public PlaybackPanel()
        {
            playString = "▶";
            pauseString = "||";
            InitializeComponent();

            waveOut = new CSCore.SoundOut.WaveOut();

            playButton.Content = playString;
            playButton.Foreground = new SolidColorBrush(Colors.Black);
            stopButton.Foreground = new SolidColorBrush(Colors.Black);

            waveOut.Stopped += SongFinished;

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

        private int reentryBlock = 0;

        public void SongFinished(object sender, CSCore.SoundOut.PlaybackStoppedEventArgs e)
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
            switch (waveOut.PlaybackState)
            {
                case CSCore.SoundOut.PlaybackState.Stopped:
                    //Do nothing
                    break;
                case CSCore.SoundOut.PlaybackState.Playing:
                case CSCore.SoundOut.PlaybackState.Paused:
                    ManualStop();
                    break;
                default:
                    ManualStop();
                    Console.WriteLine("Unexpeted playbackState: " + waveOut.PlaybackState);
                    return;
            }
        }

        public void PlaySong(DataStructures.PlayData playData)
        {
            lastPlay = playData;

            switch (waveOut.PlaybackState)
            {
                case CSCore.SoundOut.PlaybackState.Stopped:
                    //Do nothing
                    break;
                case CSCore.SoundOut.PlaybackState.Playing:
                case CSCore.SoundOut.PlaybackState.Paused:
                    ManualStop();
                    break;
                default:
                    ManualStop();
                    Console.WriteLine("Unexpeted playbackState: " + waveOut.PlaybackState);
                    return;
            }

            if (string.IsNullOrEmpty(playData.filename))
            {
                return;
            }

            songLabel.Content = String.Format("{0} - {1}", playData.artistName, playData.songTitle);
            if(waveOut.WaveSource != null)
            {
                waveOut.WaveSource.Dispose();
            }
            waveOut.Initialize(new CSCore.Codecs.MP3.DmoMp3Decoder(System.IO.File.OpenRead(playData.filename)));
            waveOut.Play();

            stopButton.Foreground = new SolidColorBrush(Colors.Red);
            playButton.Foreground = new SolidColorBrush(Colors.LightGreen);
            playButton.Content = pauseString;

            progressSlider.TickFrequency = 0.25 * waveOut.WaveSource.WaveFormat.BytesPerSecond;
            progressSlider.Maximum = waveOut.WaveSource.Length;
        }

        public void OnPlayClick(object sender, RoutedEventArgs e)
        {
            switch (waveOut.PlaybackState)
            {
                case CSCore.SoundOut.PlaybackState.Stopped:
                    PlaySong(lastPlay);
                    break;
                case CSCore.SoundOut.PlaybackState.Playing:
                    waveOut.Pause();
                    playButton.Content = playString;
                    break;
                case CSCore.SoundOut.PlaybackState.Paused:
                    waveOut.Play();
                    playButton.Content = pauseString;
                    break;
                default:
                    ManualStop();
                    Console.WriteLine("Unexpeted playbackState: " + waveOut.PlaybackState);
                    return;
            }
        }

        public void OnStopClick(object sender, RoutedEventArgs e)
        {
            switch (waveOut.PlaybackState)
            {
                case CSCore.SoundOut.PlaybackState.Stopped:
                    //Do nothing
                    break;
                case CSCore.SoundOut.PlaybackState.Playing:
                case CSCore.SoundOut.PlaybackState.Paused:
                    ManualStop();
                    playButton.Content = playString;
                    break;
                default:
                    ManualStop();
                    Console.WriteLine("Unexpeted playbackState: " + waveOut.PlaybackState);
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
            if (waveOut.PlaybackState == CSCore.SoundOut.PlaybackState.Playing &&
                waveOut.WaveSource.Position > 2.0f * waveOut.WaveSource.WaveFormat.BytesPerSecond)
            {
                //Restart if it's within the first 3 seconds
                waveOut.WaveSource.Position = 0;
            }
            else
            {
                BackClicked?.Invoke();
            }
        }

        private void ManualStop()
        {
            ++reentryBlock;
            waveOut.Stop();
            --reentryBlock;
        }

        private void Tick_ProgressTimer(object s, EventArgs e)
        {
            switch (waveOut.PlaybackState)
            {
                case CSCore.SoundOut.PlaybackState.Stopped:
                    //Do nothing
                    break;
                case CSCore.SoundOut.PlaybackState.Playing:
                case CSCore.SoundOut.PlaybackState.Paused:
                    progressSlider.Value = waveOut.WaveSource.Position;
                    break;
                default:
                    Console.WriteLine("Unexpeted playbackState: " + waveOut.PlaybackState);
                    return;
            }

            if (prepareNext)
            {
                prepareNext = false;
                PlaybackFinished?.Invoke();
            }
        }

        private void Slider_Drag(object s, DragDeltaEventArgs e)
        {
            switch (waveOut.PlaybackState)
            {
                case CSCore.SoundOut.PlaybackState.Stopped:
                    //Do nothing
                    break;
                case CSCore.SoundOut.PlaybackState.Playing:
                case CSCore.SoundOut.PlaybackState.Paused:
                    waveOut.WaveSource.Position = (long)progressSlider.Value;
                    break;
                default:
                    Console.WriteLine("Unexpeted playbackState: " + waveOut.PlaybackState);
                    return;
            }
        }
    }
}
