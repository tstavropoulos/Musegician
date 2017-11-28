using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MusicPlayer.Player
{
    /// <summary>
    /// Interaction logic for PlayerPanel.xaml
    /// </summary>
    public partial class PlayerPanel : UserControl
    {
        CSCore.SoundOut.WaveOut waveOut = null;

        DataStructures.PlayData lastPlay;

        string playString;
        string pauseString;

        public PlayerPanel()
        {
            playString = "▶";
            pauseString = "||";
            InitializeComponent();

            waveOut = new CSCore.SoundOut.WaveOut();

            playButton.Content = playString;
            playButton.Foreground = new SolidColorBrush(Colors.Black);
            stopButton.Foreground = new SolidColorBrush(Colors.Black);

            waveOut.Stopped += SongFinished;
        }

        public delegate void ClickEvent();

        public event ClickEvent NextClicked;
        public event ClickEvent BackClicked;

        private bool reentryBlock = false;

        public void SongFinished(object sender, CSCore.SoundOut.PlaybackStoppedEventArgs e)
        {
            if(!reentryBlock)
            {
                Dispatcher.Invoke(() =>
                {
                    playButton.Content = playString;
                    playButton.Foreground = new SolidColorBrush(Colors.LightGreen);
                    stopButton.Foreground = new SolidColorBrush(Colors.Black);
                });
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

            if (string.IsNullOrEmpty(playData.fileName))
            {
                return;
            }

            songLabel.Content = String.Format("{0} - {1}", playData.artistName, playData.songTitle);

            waveOut.Initialize(new CSCore.Codecs.MP3.DmoMp3Decoder(System.IO.File.OpenRead(playData.fileName)));
            waveOut.Play();

            stopButton.Foreground = new SolidColorBrush(Colors.Red);
            playButton.Foreground = new SolidColorBrush(Colors.LightGreen);
            playButton.Content = pauseString;
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
            reentryBlock = true;
            waveOut.Stop();
            reentryBlock = false;
        }
    }
}
