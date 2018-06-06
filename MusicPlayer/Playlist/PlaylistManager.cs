using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Musegician.Database;
using Musegician.Core;

namespace Musegician.Playlist
{
    public struct PlayHistory
    {
        public PlaylistSong song;
        public PlaylistRecording recording;
    }

    public class PlaylistManager : INotifyPropertyChanged
    {
        private Random random = new Random();
        private readonly List<PlaylistSong> PlaylistSongs = new List<PlaylistSong>();
        private DepletableBag<PlaylistSong> shuffleSet;

        /// <summary>
        /// A buffer holding the last 30 songs to play in Shuffle mode
        /// </summary>
        private RingBuffer<PlayHistory> ringBuffer = new RingBuffer<PlayHistory>(30);

        private int bufferIndex = 0;

        private IPlaylistRequestHandler RequestHandler => FileManager.Instance;

        private List<WeakReference<IPlaylistUpdateListener>> _playlistUpdateListeners =
            new List<WeakReference<IPlaylistUpdateListener>>();

        private string _playlistName = "";
        public string PlaylistName
        {
            get { return _playlistName; }
            set
            {
                if (_playlistName != value)
                {
                    _playlistName = value;
                    OnPropertyChanged("PlaylistName");
                }
            }
        }

        private static bool _shuffle = true;
        public bool Shuffle
        {
            get { return _shuffle; }
            set
            {
                if (_shuffle != value)
                {
                    _shuffle = value;

                    if (value)
                    {
                        //Reshuffle list
                        RebuildShuffleList();
                    }

                    OnPropertyChanged("Shuffle");
                }
            }
        }

        private static bool _repeat = true;
        public bool Repeat
        {
            get { return _repeat; }
            set
            {
                if (_repeat != value)
                {
                    _repeat = value;
                    OnPropertyChanged("Repeat");
                }
            }
        }

        public int ItemCount => PlaylistSongs.Count();


        private int _currentIndex = -1;
        public int CurrentIndex
        {
            get { return _currentIndex; }
            set
            {
                _currentIndex = value;

                foreach (IPlaylistUpdateListener listener in GetValidListeners())
                {
                    listener.MarkIndex(_currentIndex);
                }
            }
        }

        private PlaylistRecording _lastRecording = null;
        public PlaylistRecording LastRecording
        {
            get { return _lastRecording; }
            set
            {
                _lastRecording = value;

                foreach (IPlaylistUpdateListener listener in GetValidListeners())
                {
                    listener.MarkRecording(_lastRecording);
                }
            }
        }

        private static object m_lock = new object();
        private static volatile PlaylistManager _instance;
        public static PlaylistManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (m_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new PlaylistManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private PlaylistManager()
        {
            shuffleSet = new DepletableBag<PlaylistSong>(random);
        }

        public void PlaySong(PlaylistSong song)
        {
            if (PlaylistSongs.Contains(song))
            {
                
                CurrentIndex = PlaylistSongs.IndexOf(song);
                PlaylistRecording recording = SelectRecording(song);

                //Add the item to the current shuffle buffer, and remove from shuffleList
                if (Shuffle)
                {
                    shuffleSet.DepleteValue(song);

                    //Pop off history elements in front of it - We have branched our history
                    while (bufferIndex > 0)
                    {
                        --bufferIndex;
                        ringBuffer.RemoveAt(bufferIndex);
                    }

                    bufferIndex = 0;

                    ringBuffer.Add(new PlayHistory
                    {
                        song = song,
                        recording = recording
                    });
                }

                PlayRecording(recording.Recording);
            }
        }

        private void PlayRecording(Recording recording)
        {
            Player.MusicManager.Instance.PlaySong(
                playData: RequestHandler.GetRecordingPlayData(recording));
        }

        public void PlayRecording(PlaylistSong song, PlaylistRecording recording)
        {
            if (PlaylistSongs.Contains(song))
            {
                CurrentIndex = PlaylistSongs.IndexOf(song);
                LastRecording = recording;

                //Add the item to the current shuffle buffer, and remove from shuffleList
                if (Shuffle)
                {
                    shuffleSet.DepleteValue(song);

                    //Pop off history elements in front of it
                    while (bufferIndex > 0)
                    {
                        --bufferIndex;
                        ringBuffer.RemoveAt(bufferIndex);
                    }

                    bufferIndex = 0;

                    ringBuffer.Add(new PlayHistory
                    {
                        song = song,
                        recording = recording
                    });
                }

                PlayRecording(recording.Recording);
            }
        }

        public DataStructures.PlayData Next()
        {
            if (ItemCount == 0)
            {
                return new DataStructures.PlayData();
            }

            if (Shuffle)
            {
                //Shuffle Logic

                //If we had backtracked, we retread before continuing to shuffle
                //First, cull elements who are now invalid
                while (bufferIndex > 0)
                {
                    --bufferIndex;

                    if (!PlaylistSongs.Contains(ringBuffer[bufferIndex].song))
                    {
                        //Bad values - Song not found - Probably Deleted

                        ringBuffer.RemoveAt(bufferIndex);
                        continue;
                    }

                    if (!ringBuffer[bufferIndex].song.PlaylistRecordings.Contains(ringBuffer[bufferIndex].recording))
                    {
                        //Bad Values - Recording not found - Probably Deleted

                        ringBuffer.RemoveAt(bufferIndex);
                        continue;
                    }

                    //int recordingIndex = playlist[index].Children.IndexOf(ringBuffer[bufferIndex].recording);

                    //Update vales to select the correct song in the UI
                    CurrentIndex = PlaylistSongs.IndexOf(ringBuffer[bufferIndex].song);
                    LastRecording = ringBuffer[bufferIndex].recording;

                    return RequestHandler.GetRecordingPlayData(LastRecording.Recording);
                }

                //Move on to a new song

                //Track the number of reshuffles, in case the user has a stupid setup
                int reshuffles = 0;

                //Select the next song if we're not out of songs yet
                while (shuffleSet.Count > 0 || Repeat)
                {
                    //Reshuffle list if Repeat is turned on
                    if (shuffleSet.Count == 0 && Repeat)
                    {
                        shuffleSet.Reset();

                        if (++reshuffles >= 3)
                        {
                            MessageBox.Show(
                                messageBoxText: "Reshuffled list twice without playing a song.\n" +
                                    "Try Increasing Weights.",
                                caption: "Error",
                                button: MessageBoxButton.OK,
                                icon: MessageBoxImage.Warning);

                            return new DataStructures.PlayData();
                        }
                    }

                    PlaylistSong nextSong = shuffleSet.PopNext();

                    if (!TestSongWeight(nextSong))
                    {
                        //Skip it if the selected song fails a weight test
                        continue;
                    }

                    //Song passed weight test
                    CurrentIndex = PlaylistSongs.IndexOf(nextSong);
                    PlaylistRecording recording = SelectRecording(nextSong);

                    //Stash 
                    ringBuffer.Add(new PlayHistory
                    {
                        song = nextSong,
                        recording = recording
                    });

                    return RequestHandler.GetRecordingPlayData(recording.Recording);
                }
            }
            else
            {
                ringBuffer.Clear();
                bufferIndex = 0;

                int nextPlaylistIndex = CurrentIndex;

                //Track the number of restarts, in case the user has a stupid setup
                int restarts = 0;

                while (nextPlaylistIndex < ItemCount || Repeat)
                {
                    ++nextPlaylistIndex;

                    if (nextPlaylistIndex >= ItemCount)
                    {
                        //Only true if Repeat

                        //Start from top of list
                        nextPlaylistIndex = 0;

                        if (++restarts >= 3)
                        {
                            MessageBox.Show(
                                messageBoxText: "Restarted list twice without playing a song.\n" +
                                    "Try Increasing Weights.",
                                caption: "Error",
                                button: MessageBoxButton.OK,
                                icon: MessageBoxImage.Warning);

                            return new DataStructures.PlayData();
                        }
                    }

                    PlaylistSong nextSong = PlaylistSongs[nextPlaylistIndex];

                    //Test the next song to see if its weight passes muster
                    if (!TestSongWeight(nextSong))
                    {
                        //Skip it if the selected song fails a weight test
                        continue;
                    }

                    //Song passed weight test
                    CurrentIndex = nextPlaylistIndex;

                    return RequestHandler.GetRecordingPlayData(
                        SelectRecording(nextSong).Recording);
                }
            }

            return new DataStructures.PlayData();
        }

        public DataStructures.PlayData Previous()
        {
            if (ItemCount == 0)
            {
                return new DataStructures.PlayData();
            }

            if (Shuffle)
            {
                //Shuffle logic

                //Use the ringbuffer until it runs out
                while (bufferIndex < ringBuffer.Count - 1)
                {
                    ++bufferIndex;

                    if (!PlaylistSongs.Contains(ringBuffer[bufferIndex].song))
                    {
                        //Bad values - Song not found - Probably Deleted

                        ringBuffer.RemoveAt(bufferIndex);
                        continue;
                    }

                    if (!ringBuffer[bufferIndex].song.PlaylistRecordings.Contains(ringBuffer[bufferIndex].recording))
                    {
                        //Bad Values - Recording not found - Probably Deleted

                        ringBuffer.RemoveAt(bufferIndex);
                        continue;
                    }

                    //Update vales to select the correct song in the UI
                    CurrentIndex = PlaylistSongs.IndexOf(ringBuffer[bufferIndex].song);
                    LastRecording = ringBuffer[bufferIndex].recording;

                    return RequestHandler.GetRecordingPlayData(LastRecording.Recording);
                }
            }
            else
            {
                ringBuffer.Clear();
                bufferIndex = 0;

                //Linear logic
                int nextPlaylistIndex = CurrentIndex;

                //Track the number of restarts, in case the user has a stupid setup
                int restarts = 0;

                while (nextPlaylistIndex > 0 || Repeat)
                {
                    --nextPlaylistIndex;

                    //Restart index if Repeat is on
                    if (nextPlaylistIndex < 0)
                    {
                        nextPlaylistIndex = ItemCount - 1;

                        if (++restarts >= 3)
                        {
                            MessageBox.Show(
                                messageBoxText: "Restarted list twice without playing a song.\n" +
                                    "Try Increasing Weights.",
                                caption: "Error",
                                button: MessageBoxButton.OK,
                                icon: MessageBoxImage.Warning);

                            return new DataStructures.PlayData();
                        }
                    }

                    PlaylistSong nextSong = PlaylistSongs[nextPlaylistIndex];

                    //Test the next song to see if its weight passes muster
                    if (!TestSongWeight(nextSong))
                    {
                        //Skip it if the selected song fails a weight test
                        continue;
                    }

                    //Song passed weight test
                    CurrentIndex = nextPlaylistIndex;

                    return RequestHandler.GetRecordingPlayData(
                        SelectRecording(nextSong).Recording);
                }
            }

            return new DataStructures.PlayData();
        }

        public void Rebuild(IEnumerable<PlaylistSong> songs)
        {
            _currentIndex = -1;
            PlaylistSongs.Clear();
            int index = 0;
            foreach (PlaylistSong song in songs)
            {
                song.Number = index++;
                PlaylistSongs.Add(song);
            }
            RebuildShuffleList();

            ringBuffer.Clear();
            bufferIndex = 0;

            foreach (IPlaylistUpdateListener listener in GetValidListeners())
            {
                listener.Rebuild(songs);
            }
        }

        public void AddBack(IEnumerable<PlaylistSong> songs)
        {
            foreach (PlaylistSong song in songs)
            {
                PlaylistSongs.Add(song);
            }

            shuffleSet.AddRange(songs);

            foreach (IPlaylistUpdateListener listener in GetValidListeners())
            {
                listener.AddBack(songs);
            }
        }

        public void InsertSongs(int position, IEnumerable<PlaylistSong> songs)
        {
            if (position == -1)
            {
                AddBack(songs);
            }
            else
            {
                position = Math.Min(position, PlaylistSongs.Count);
                PlaylistSongs.InsertRange(position, songs);
                shuffleSet.AddRange(songs);
            }

            foreach (IPlaylistUpdateListener listener in GetValidListeners())
            {
                listener.InsertSongs(position, songs);
            }
        }

        public void RemoveIndex(IEnumerable<int> songIndices)
        {
            List<int> reverseSortedIndices = new List<int>(songIndices);
            reverseSortedIndices.Sort((a, b) => b.CompareTo(a));

            foreach (int songIndex in reverseSortedIndices)
            {
                if (songIndex < CurrentIndex)
                {
                    --_currentIndex;
                }

                if (shuffleSet.Contains(PlaylistSongs[songIndex]))
                {
                    shuffleSet.Remove(PlaylistSongs[songIndex]);
                }

                PlaylistSongs.RemoveAt(songIndex);
            }

            foreach (IPlaylistUpdateListener listener in GetValidListeners())
            {
                listener.RemoveIndices(songIndices);
            }
        }

        private PlaylistRecording SelectRecording(PlaylistSong song)
        {
            if (song.PlaylistRecordings.Count == 0)
            {
                LastRecording = null;
                return null;
            }

            if (song.PlaylistRecordings.Count == 1)
            {
                LastRecording = song.PlaylistRecordings.First();
                return LastRecording;
            }

            double cumulativeWeight = 0.0;

            foreach (PlaylistRecording recording in song.PlaylistRecordings)
            {
                cumulativeWeight += GetWeight(recording);
            }

            cumulativeWeight *= random.NextDouble();

            foreach (PlaylistRecording recording in song.PlaylistRecordings)
            {
                cumulativeWeight -= GetWeight(recording);
                if (cumulativeWeight <= 0.0)
                {
                    LastRecording = recording;
                    return recording;
                }
            }

            Console.WriteLine("Failed to pick a recording before running out.  Method is broken.");
            LastRecording = song.PlaylistRecordings.First();
            return LastRecording;
        }

        private double GetWeight(PlaylistRecording recording)
        {
            if (recording.Weight == -1.0)
            {
                if (recording.Recording.Live)
                {
                    return Settings.Instance.LiveWeight;
                }
                else
                {
                    return Settings.Instance.StudioWeight;
                }
            }

            return recording.Weight;
        }

        public void RebuildShuffleList()
        {
            shuffleSet.Clear();
            shuffleSet.AddRange(PlaylistSongs);
        }

        public void ClearPlaylist()
        {
            Rebuild(new List<PlaylistSong>());
            PlaylistName = "";
        }

        public void TryLoadPlaylist(string playlistTitle)
        {
            Rebuild(RequestHandler.LoadPlaylist(playlistTitle));
            PlaylistName = playlistTitle;
        }

        public void SavePlaylistAs(string title)
        {
            if (PlaylistSongs.Count != 0)
            {
                RequestHandler.SavePlaylistAs(title, PlaylistSongs);
                PlaylistName = title;
            }
        }

        public void DeletePlaylist(string playlistTitle)
        {
            RequestHandler.DeletePlaylist(playlistTitle);
        }

        public void AppendPlaylist(string playlistTitle)
        {
            AddBack(RequestHandler.LoadPlaylist(playlistTitle));
        }

        /// <summary>
        /// Use the weight of a song (at playlistIndex) to determine if it should play.
        /// Probability is equal to its weight.
        /// </summary>
        /// <param name="playlistIndex"></param>
        /// <returns></returns>
        private bool TestSongWeight(PlaylistSong song)
        {
            double weight = (song.Weight != -1.0) ? song.Weight : song.DefaultWeight;

            if (weight.AlmostEqualRelative(1.0))
            {
                return true;
            }

            if (weight.AlmostEqualRelative(0.0))
            {
                return false;
            }

            return random.NextDouble() <= weight;
        }

        public void BatchRearrangeSongs(IEnumerable<int> sourceIndices, int targetIndex)
        {
            int preTargetMoves = 0;
            int postTargetMoves = 0;

            foreach (int sourceIndex in sourceIndices)
            {
                if (sourceIndex < targetIndex)
                {
                    PlaylistSongs.Insert(targetIndex, PlaylistSongs[sourceIndex - preTargetMoves]);
                    PlaylistSongs.RemoveAt(sourceIndex - preTargetMoves);

                    ++preTargetMoves;
                }
                else if (sourceIndex == targetIndex)
                {
                    ++postTargetMoves;
                }
                else // (sourceIndex > targetIndex)
                {
                    PlaylistSongs.Insert(targetIndex + postTargetMoves, PlaylistSongs[sourceIndex]);
                    PlaylistSongs.RemoveAt(sourceIndex + 1);
                    ++postTargetMoves;
                }
            }

            foreach (IPlaylistUpdateListener listener in GetValidListeners())
            {
                listener.Rearrange(sourceIndices, targetIndex);
            }
        }

        #region PlaylistUpdateListener

        private IEnumerable<IPlaylistUpdateListener> GetValidListeners()
        {
            //Clean up dead weak references
            _playlistUpdateListeners.RemoveAll(x => !x.TryGetTarget(out IPlaylistUpdateListener y));

            foreach (var weakListener in _playlistUpdateListeners)
            {
                if (weakListener.TryGetTarget(out IPlaylistUpdateListener target) && target != null)
                {
                    yield return target;
                }
            }
        }

        public void AddListener(IPlaylistUpdateListener listener)
        {
            //Check that it wasn't already added
            foreach (IPlaylistUpdateListener target in GetValidListeners())
            {
                if (target == listener)
                {
                    //Target already exists, bail
                    return;
                }
            }

            _playlistUpdateListeners.Add(new WeakReference<IPlaylistUpdateListener>(listener));

            //Newly added listeners immediately have Rebuild, Mark, and Mark Recording called
            listener.Rebuild(PlaylistSongs);

            if (CurrentIndex != -1)
            {
                listener.MarkIndex(CurrentIndex);
            }

            if (LastRecording != null)
            {
                listener.MarkRecording(LastRecording);
            }
        }

        public void RemoveListener(IPlaylistUpdateListener listener)
        {
            _playlistUpdateListeners.RemoveAll(
                weakRef => !weakRef.TryGetTarget(out IPlaylistUpdateListener target) || target == listener);
        }

        #endregion PlaylistUpdateListener
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}
