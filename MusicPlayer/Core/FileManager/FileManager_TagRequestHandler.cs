using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SQLite;
using System.Text;
using System.Threading.Tasks;
using Musegician.Database;
using Musegician.DataStructures;
using Musegician.TagEditor;

using LibraryContext = Musegician.Library.LibraryContext;

namespace Musegician
{
    public partial class FileManager : ITagRequestHandler
    {
        #region ITagRequestHandler

        IEnumerable<TagData> ITagRequestHandler.GetTagData(BaseData data)
        {
            List<TagData> tagList = new List<TagData>();

            if (data is Artist artist)
            {
                tagList.Add(new TagDataString
                {
                    _currentValue = artist.Name,
                    NewValue = artist.Name,
                    recordType = MusicRecord.ArtistName,
                    tagType = ID3TagType.Performer,
                    tagTypeIndex = 0
                });
            }
            else if (data is Album album)
            {
                tagList.Add(new TagDataString
                {
                    _currentValue = album.Title,
                    NewValue = album.Title,
                    recordType = MusicRecord.AlbumTitle,
                    tagType = ID3TagType.Album
                });

                tagList.Add(new TagDataInt
                {
                    _currentValue = album.Year,
                    _newValue = album.Year,
                    recordType = MusicRecord.AlbumYear,
                    tagType = ID3TagType.Year
                });
            }
            else if (data is Song song)
            {
                tagList.Add(new TagDataString
                {
                    _currentValue = song.Title,
                    NewValue = song.Title,
                    recordType = MusicRecord.SongTitle
                });
            }
            else if (data is Track track)
            {
                tagList.Add(new TagViewable()
                {
                    _CurrentValue = track.Recording.Filename,
                    recordType = MusicRecord.Filename
                });

                tagList.Add(new TagDataBool()
                {
                    _currentValue = track.Recording.Live,
                    NewValue = track.Recording.Live,
                    recordType = MusicRecord.Live
                });

                tagList.Add(new TagDataString
                {
                    _currentValue = track.Title,
                    NewValue = track.Title,
                    recordType = MusicRecord.TrackTitle,
                    tagType = ID3TagType.Title
                });

                tagList.Add(new TagDataString
                {
                    _currentValue = track.Recording.Song.Title,
                    NewValue = track.Recording.Song.Title,
                    recordType = MusicRecord.SongTitle
                });

                tagList.Add(new TagDataInt
                {
                    _currentValue = track.TrackNumber,
                    _newValue = track.TrackNumber,
                    recordType = MusicRecord.TrackNumber,
                    tagType = ID3TagType.Track
                });

                tagList.Add(new TagDataInt
                {
                    _currentValue = track.DiscNumber,
                    _newValue = track.DiscNumber,
                    recordType = MusicRecord.DiscNumber,
                    tagType = ID3TagType.Disc
                });

                tagList.Add(new TagDataInt
                {
                    _currentValue = track.Album.Year,
                    _newValue = track.Album.Year,
                    recordType = MusicRecord.AlbumYear,
                    tagType = ID3TagType.Year
                });

                tagList.Add(new TagDataString
                {
                    _currentValue = track.Recording.Artist.Name,
                    NewValue = track.Recording.Artist.Name,
                    recordType = MusicRecord.ArtistName,
                    tagType = ID3TagType.Performer,
                    tagTypeIndex = 0
                });
            }
            else if (data is Recording recording)
            {
                tagList.Add(new TagViewable()
                {
                    _CurrentValue = recording.Filename,
                    recordType = MusicRecord.Filename
                });

                tagList.Add(new TagDataBool()
                {
                    _currentValue = recording.Live,
                    NewValue = recording.Live,
                    recordType = MusicRecord.Live
                });

                tagList.Add(new TagDataString
                {
                    _currentValue = recording.Song.Title,
                    NewValue = recording.Song.Title,
                    recordType = MusicRecord.SongTitle
                });

                tagList.Add(new TagDataString
                {
                    _currentValue = recording.Artist.Name,
                    NewValue = recording.Artist.Name,
                    recordType = MusicRecord.ArtistName,
                    tagType = ID3TagType.Performer,
                    tagTypeIndex = 0
                });
            }

            return tagList;
        }


        IEnumerable<TagData> ITagRequestHandler.GetTagData(IEnumerable<BaseData> data)
        {
            List<TagData> tagList = new List<TagData>();

            BaseData firstDatum = data.First();

            if (firstDatum is Artist artist)
            {
                tagList.Add(new TagDataString
                {
                    _currentValue = artist.Name,
                    NewValue = artist.Name,
                    recordType = MusicRecord.ArtistName,
                    tagType = ID3TagType.Performer,
                    tagTypeIndex = 0
                });
            }
            else if (firstDatum is Album album)
            {
                tagList.Add(new TagDataString
                {
                    _currentValue = album.Title,
                    NewValue = album.Title,
                    recordType = MusicRecord.AlbumTitle,
                    tagType = ID3TagType.Album
                });

                tagList.Add(new TagDataInt
                {
                    _currentValue = album.Year,
                    _newValue = album.Year,
                    recordType = MusicRecord.AlbumYear,
                    tagType = ID3TagType.Year
                });
            }
            else if (firstDatum is Song song)
            {
                tagList.Add(new TagDataString
                {
                    _currentValue = song.Title,
                    NewValue = song.Title,
                    recordType = MusicRecord.SongTitle
                });
            }
            else if (firstDatum is Track track)
            {
                tagList.Add(new TagViewable()
                {
                    _CurrentValue = track.Recording.Filename,
                    recordType = MusicRecord.Filename
                });

                tagList.Add(new TagDataBool()
                {
                    _currentValue = track.Recording.Live,
                    NewValue = track.Recording.Live,
                    recordType = MusicRecord.Live
                });

                tagList.Add(new TagDataString
                {
                    _currentValue = track.Title,
                    NewValue = track.Title,
                    recordType = MusicRecord.TrackTitle,
                    tagType = ID3TagType.Title
                });

                tagList.Add(new TagDataString
                {
                    _currentValue = track.Recording.Song.Title,
                    NewValue = track.Recording.Song.Title,
                    recordType = MusicRecord.SongTitle
                });

                tagList.Add(new TagDataInt
                {
                    _currentValue = track.TrackNumber,
                    _newValue = track.TrackNumber,
                    recordType = MusicRecord.TrackNumber,
                    tagType = ID3TagType.Track
                });

                tagList.Add(new TagDataInt
                {
                    _currentValue = track.DiscNumber,
                    _newValue = track.DiscNumber,
                    recordType = MusicRecord.DiscNumber,
                    tagType = ID3TagType.Disc
                });

                tagList.Add(new TagDataInt
                {
                    _currentValue = track.Album.Year,
                    _newValue = track.Album.Year,
                    recordType = MusicRecord.AlbumYear,
                    tagType = ID3TagType.Year
                });

                tagList.Add(new TagDataString
                {
                    _currentValue = track.Recording.Artist.Name,
                    NewValue = track.Recording.Artist.Name,
                    recordType = MusicRecord.ArtistName,
                    tagType = ID3TagType.Performer,
                    tagTypeIndex = 0
                });
            }
            else if (firstDatum is Recording recording)
            {
                tagList.Add(new TagViewable()
                {
                    _CurrentValue = recording.Filename,
                    recordType = MusicRecord.Filename
                });

                tagList.Add(new TagDataBool()
                {
                    _currentValue = recording.Live,
                    NewValue = recording.Live,
                    recordType = MusicRecord.Live
                });

                tagList.Add(new TagDataString
                {
                    _currentValue = recording.Song.Title,
                    NewValue = recording.Song.Title,
                    recordType = MusicRecord.SongTitle
                });

                tagList.Add(new TagDataString
                {
                    _currentValue = recording.Artist.Name,
                    NewValue = recording.Artist.Name,
                    recordType = MusicRecord.ArtistName,
                    tagType = ID3TagType.Performer,
                    tagTypeIndex = 0
                });
            }

            return tagList;
        }

        /// <summary>
        /// Identifies all of the files potentially requiring ID3 tag updates
        /// </summary>
        IEnumerable<string> ITagRequestHandler.GetAffectedFiles(BaseData data)
        {
            if (data is Artist artist)
            {
                return artist.Recordings.Distinct().Select(x => x.Filename);
            }
            else if (data is Album album)
            {
                return album.Tracks.Select(x => x.Recording).Distinct().Select(x => x.Filename);
            }
            else if (data is Song song)
            {
                return song.Recordings.Distinct().Select(x => x.Filename);
            }
            else if (data is Track track)
            {
                return new string[] { track.Recording.Filename };
            }
            else if (data is Recording recording)
            {
                return new string[] { recording.Filename };
            }

            Console.WriteLine("Unable to identify BaseData Type: " + data);

            return null;
        }

        /// <summary>
        /// Identifies all of the files potentially requiring ID3 tag updates
        /// </summary>
        IEnumerable<string> ITagRequestHandler.GetAffectedFiles(IEnumerable<BaseData> data)
        {
            BaseData firstDatum = data.First();
            if (firstDatum is Artist)
            {
                return data.SelectMany(x => (x as Artist).Recordings).Distinct().Select(x => x.Filename);
            }
            else if (firstDatum is Album)
            {
                return data.SelectMany(x => (x as Album).Tracks).Select(x => x.Recording).Distinct().Select(x => x.Filename);
            }
            else if (firstDatum is Song)
            {
                return data.SelectMany(x => (x as Song).Recordings).Distinct().Select(x => x.Filename);
            }
            else if (firstDatum is Track)
            {
                return data.Select(x => (x as Track).Recording).Distinct().Select(x => x.Filename);
            }
            else if (firstDatum is Recording)
            {
                return data.Select(x => (x as Recording).Filename);
            }

            Console.WriteLine("Unable to identify BaseData Type: " + firstDatum);

            return null;
        }

        /// <summary>
        /// Make sure to translate the ID to the right context before calling this method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="id"></param>
        /// <param name="record"></param>
        /// <param name="newString"></param>
        /// <exception cref="LibraryContextException"/>
        void ITagRequestHandler.UpdateRecord(
            IEnumerable<BaseData> data,
            MusicRecord record,
            string newString)
        {
            if (data.Count() == 0)
            {
                throw new InvalidOperationException(
                    $"Found 0 records to modify for MusicRecord {record.ToString()}");
            }

            BaseData firstDatum = data.First();

            switch (record)
            {
                case MusicRecord.SongTitle:
                    {
                        if (firstDatum is Song)
                        {
                            //Renaming (or Consolidating) Songs
                            songCommands.UpdateSongTitle(data.Select(x => x as Song), newString);
                        }
                        else if (firstDatum is Track)
                        {
                            //Splitting, Renaming, And/Or Consolidating Tracks by Song Title
                            trackCommands.UpdateSongTitle(data.Select(x => x as Track), newString);
                        }
                        else if (firstDatum is Recording)
                        {
                            //Splitting, Renaming, And/Or Consolidating Tracks by Song Title
                            recordingCommands.UpdateSongTitle(data.Select(x => x as Recording), newString);
                        }
                        else
                        {
                            throw new LibraryContextException(
                                $"Bad Context ({firstDatum}) for RecordUpdate ({record.ToString()})");
                        }
                    }
                    break;
                case MusicRecord.ArtistName:
                    {
                        if (firstDatum is Artist)
                        {
                            //Renaming and collapsing Artists
                            artistCommands.UpdateArtistName(data.Select(x => x as Artist), newString);
                        }
                        else if (firstDatum is Track)
                        {
                            //Assinging Songs to a different artist
                            trackCommands.UpdateArtistName(data.Select(x => x as Track), newString);
                        }
                        else if (firstDatum is Recording)
                        {
                            //Assinging Songs to a different artist
                            recordingCommands.UpdateArtistName(data.Select(x => x as Recording), newString);
                        }
                        else
                        {
                            throw new LibraryContextException(
                                $"Bad Context ({firstDatum}) for RecordUpdate ({record.ToString()})");
                        }

                    }
                    break;
                case MusicRecord.AlbumTitle:
                    {
                        if (firstDatum is Album)
                        {
                            //Renaming and collapsing Albums
                            albumCommands.UpdateAlbumTitle(data.Select(x => x as Album), newString);
                        }
                        else if (firstDatum is Track)
                        {
                            //Assigning a track to a different album
                            trackCommands.UpdateAlbumTitle(data.Select(x => x as Track), newString);
                        }
                        else
                        {
                            throw new LibraryContextException(
                                $"Bad Context ({firstDatum}) for RecordUpdate ({record.ToString()})");
                        }
                    }
                    break;
                case MusicRecord.TrackTitle:
                    {
                        if (firstDatum is Track)
                        {
                            //Renaming and collapsing Albums
                            trackCommands.UpdateTrackTitle(data.Select(x => x as Track), newString);
                        }
                        else
                        {
                            throw new LibraryContextException(
                                $"Bad Context ({firstDatum}) for RecordUpdate ({record.ToString()})");
                        }
                    }
                    break;
                default:
                    throw new Exception(string.Format(
                        "Wrong field type submitted. Submitted {0} for field {1}.",
                        newString.GetType().ToString(),
                        record.ToString()));
            }
        }

        void ITagRequestHandler.UpdateRecord(
            IEnumerable<BaseData> data,
            MusicRecord record,
            int newInt)
        {

            if (data.Count() == 0)
            {
                throw new InvalidOperationException(
                    $"Found 0 records to modify for MusicRecord {record.ToString()}");
            }

            BaseData firstDatum = data.First();
            switch (record)
            {
                case MusicRecord.TrackNumber:
                    {
                        if (firstDatum is Track)
                        {
                            //Updating the track number of a track
                            trackCommands.UpdateTrackNumber(data.Select(x => x as Track), newInt);
                        }
                        else
                        {
                            throw new LibraryContextException(
                                $"Bad Context ({firstDatum}) for RecordUpdate ({record.ToString()})");
                        }
                    }
                    break;
                case MusicRecord.AlbumYear:
                    {
                        if (firstDatum is Album)
                        {
                            //Updating the year that an album was produced
                            albumCommands.UpdateYear(data.Select(x => x as Album), newInt);
                        }
                        else
                        {
                            throw new LibraryContextException(
                                $"Bad Context ({firstDatum}) for RecordUpdate ({record.ToString()})");
                        }
                    }
                    break;
                case MusicRecord.DiscNumber:
                    {
                        if (firstDatum is Track)
                        {
                            //Updating the disc that a track appeared on
                            trackCommands.UpdateDisc(data.Select(x => x as Track), newInt);
                        }
                        else
                        {
                            throw new LibraryContextException(
                                $"Bad Context ({firstDatum}) for RecordUpdate ({record.ToString()})");
                        }
                    }
                    break;
                default:
                    throw new Exception(string.Format(
                        "Wrong field type submitted. Submitted {0} for field {1}.",
                        newInt.GetType().ToString(),
                        record.ToString()));
            }
        }

        void ITagRequestHandler.UpdateRecord(
            IEnumerable<BaseData> data,
            MusicRecord record,
            bool newBool)
        {
            if (data.Count() == 0)
            {
                throw new InvalidOperationException(
                    $"Found 0 records to modify for MusicRecord {record.ToString()}");
            }

            BaseData firstDatum = data.First();

            switch (record)
            {
                case MusicRecord.Live:
                    {
                        if (firstDatum is Track)
                        {
                            //Update Recording Live Status
                            trackCommands.UpdateLive(data.Select(x => x as Track), newBool);
                        }
                        else if (firstDatum is Recording)
                        {
                            //Update Recording Live Status
                            recordingCommands.UpdateLive(data.Select(x => x as Recording), newBool);
                        }
                        else
                        {
                            throw new LibraryContextException(
                                $"Bad Context ({firstDatum}) for RecordUpdate ({record.ToString()})");
                        }
                    }
                    break;
                default:
                    throw new Exception(string.Format(
                        "Wrong field type submitted. Submitted {0} for field {1}.",
                        newBool.GetType().ToString(),
                        record.ToString()));
            }
        }

        void ITagRequestHandler.UpdateRecord(
            IEnumerable<BaseData> data,
            MusicRecord record,
            double newDouble)
        {
            if (data.Count() == 0)
            {
                throw new InvalidOperationException(
                    $"Found 0 records to modify for MusicRecord {record.ToString()}");
            }

            foreach (BaseData datum in data)
            {
                datum.Weight = newDouble;
            }
        }

        void ITagRequestHandler.PushChanges()
        {
            _rebuildNotifier?.Invoke(this, EventArgs.Empty);
        }

        #endregion ITagRequestHandler
    }
}
