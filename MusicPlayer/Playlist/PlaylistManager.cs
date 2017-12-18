using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MusicPlayer.DataStructures;

namespace MusicPlayer.Playlist
{
    public class PlaylistManager : INotifyPropertyChanged
    {
        private Random random = new Random();
        private List<SongDTO> playlist = new List<SongDTO>();
        private List<int> shuffleList = new List<int>();

        private IPlaylistRequestHandler requestHandler
        {
            get { return FileManager.Instance; }
        }

        public delegate void PassIndex(int index);

        private event PassIndex _MarkIndex;
        public event PassIndex MarkIndex
        {
            add
            {
                _MarkIndex += value;
                if (CurrentIndex != -1)
                {
                    value?.Invoke(CurrentIndex);
                }
            }
            remove { _MarkIndex -= value; }
        }

        private event PassIndex _MarkRecordingIndex;
        public event PassIndex MarkRecordingIndex
        {
            add
            {
                _MarkRecordingIndex += value;
                if (LastRecordingIndex != -1)
                {
                    value?.Invoke(LastRecordingIndex);
                }
            }
            remove { _MarkRecordingIndex -= value; }
        }

        public event PassIndex RemoveAt;

        public delegate void Notify();
        public event Notify UnmarkAll;

        public delegate void UpdateSongs(ICollection<SongDTO> songs);
        public event UpdateSongs addBack;

        private static object m_addLock = new object();
        private event UpdateSongs _rebuild;
        public event UpdateSongs rebuild
        {
            add
            {
                lock (m_addLock)
                {
                    _rebuild += value;
                    value?.Invoke(playlist);
                }
            }
            remove
            {
                lock (m_addLock)
                {
                    _rebuild -= value;
                }
            }
        }

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
                        PrepareShuffleList();
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

        public int ItemCount
        {
            get { return playlist.Count; }
        }


        private int _currentIndex = -1;
        public int CurrentIndex
        {
            get { return _currentIndex; }
            set
            {
                _currentIndex = value;
                _MarkIndex?.Invoke(_currentIndex);
            }
        }

        private int _lastRecordingIndex = -1;
        public int LastRecordingIndex
        {
            get { return _lastRecordingIndex; }
            set
            {
                _lastRecordingIndex = value;
                _MarkRecordingIndex?.Invoke(_lastRecordingIndex);
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

        }

        public void PlayIndex(int index)
        {
            if (ItemCount > index)
            {
                CurrentIndex = index;

                if (shuffleList.Contains(CurrentIndex))
                {
                    shuffleList.Remove(CurrentIndex);
                }

                PlayRecording(SelectRecording(playlist[CurrentIndex]));
            }
        }

        public void PlayRecording(int songIndex, int recordingIndex)
        {
            if (ItemCount > songIndex)
            {
                CurrentIndex = songIndex;
                LastRecordingIndex = recordingIndex;

                PlayRecording(playlist[CurrentIndex].Children[LastRecordingIndex].ID);
            }
        }

        public PlayData Next()
        {
            if (ItemCount == 0)
            {
                return new PlayData();
            }

            if (Shuffle)
            {
                if (shuffleList.Count == 0 && Repeat)
                {
                    PrepareShuffleList();
                }

                if (shuffleList.Count > 0)
                {
                    int nextIndex = random.Next(0, shuffleList.Count);
                    CurrentIndex = shuffleList[nextIndex];
                    shuffleList.RemoveAt(nextIndex);

                    return FileManager.Instance.GetRecordingPlayData(
                        SelectRecording(playlist[CurrentIndex]));
                }
            }
            else
            {
                if (ItemCount > CurrentIndex + 1)
                {
                    ++CurrentIndex;

                    return FileManager.Instance.GetRecordingPlayData(
                        SelectRecording(playlist[CurrentIndex]));
                }
            }

            return new PlayData();
        }

        public PlayData Previous()
        {
            if (CurrentIndex > 0)
            {
                --CurrentIndex;

                return FileManager.Instance.GetRecordingPlayData(
                    SelectRecording(playlist[CurrentIndex]));
            }

            return new PlayData();
        }

        public void Rebuild(ICollection<SongDTO> songs)
        {
            _currentIndex = -1;
            playlist = new List<SongDTO>(songs);
            PrepareShuffleList();

            _rebuild?.Invoke(songs);
        }

        public void AddBack(ICollection<SongDTO> songs)
        {
            int originalItemCount = ItemCount;

            playlist.AddRange(songs);

            for (int i = originalItemCount; i < ItemCount; i++)
            {
                shuffleList.Add(i);
            }

            addBack?.Invoke(songs);
        }

        public void RemoveIndex(int songIndex)
        {
            if (songIndex < CurrentIndex)
            {
                --_currentIndex;
            }

            if (shuffleList.Contains(songIndex))
            {
                shuffleList.Remove(songIndex);
            }

            for (int i = 0; i < shuffleList.Count; i++)
            {
                if (songIndex < shuffleList[i])
                {
                    --shuffleList[i];
                }
            }

            playlist.RemoveAt(songIndex);

            RemoveAt?.Invoke(songIndex);
        }

        private long SelectRecording(SongDTO song)
        {
            if (song.Children.Count == 0)
            {
                LastRecordingIndex = -1;
                return -1;
            }

            if (song.Children.Count == 1)
            {
                LastRecordingIndex = 0;
                return song.Children[LastRecordingIndex].ID;
            }


            double cumulativeWeight = 0.0;

            foreach (RecordingDTO recording in song.Children)
            {
                cumulativeWeight += GetWeight(recording);
            }

            cumulativeWeight *= random.NextDouble();

            for (int i = 0; i < song.Children.Count; i++)
            {
                RecordingDTO recording = song.Children[i] as RecordingDTO;

                cumulativeWeight -= GetWeight(recording);
                if (cumulativeWeight <= 0.0)
                {
                    LastRecordingIndex = i;
                    return recording.ID;
                }
            }

            Console.WriteLine("Failed to pick a recording before running out.  Method is broken.");
            LastRecordingIndex = 0;
            return song.Children[0].ID;
        }

        private double GetWeight(RecordingDTO recording)
        {
            if (double.IsNaN(recording.Weight))
            {
                if (recording.Live)
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

        public void PrepareShuffleList()
        {
            shuffleList.Clear();

            for (int i = 0; i < ItemCount; i++)
            {
                shuffleList.Add(i);
            }
        }

        private void PlayRecording(long recordingID)
        {
            Player.MusicManager.Instance.PlaySong(
                playData: FileManager.Instance.GetRecordingPlayData(recordingID));
        }

        public void ClearPlaylist()
        {
            Rebuild(new List<SongDTO>());

            PlaylistName = "";
        }

        public void LoadPlaylist(long playlistID)
        {
            Rebuild(requestHandler.LoadPlaylist(playlistID));
        }

        public void TryLoadPlaylist(string playlistTitle)
        {
            long playlistID = requestHandler.FindPlaylist(playlistTitle);

            if (playlistID == -1)
            {
                MessageBox.Show(
                    "Could not find playlist titled: " + playlistTitle,
                    "Playlist Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            else
            {
                LoadPlaylist(playlistID);
            }
        }

        public void SavePlaylistAs(string title)
        {
            if (playlist.Count != 0)
            {
                requestHandler.SavePlaylist(title, playlist);
            }
        }

        public void DeletePlaylist(long playlistID)
        {
            requestHandler.DeletePlaylist(
                playlistID: playlistID);
        }

        public void AppendPlaylist(long playlistID)
        {
            AddBack(requestHandler.LoadPlaylist(playlistID));
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
