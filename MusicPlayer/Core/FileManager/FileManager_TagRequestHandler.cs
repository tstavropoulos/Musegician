using System;
using System.Collections.Generic;
using System.Linq;
using Musegician.Database;
using Musegician.DataStructures;
using Musegician.TagEditor;

namespace Musegician
{
    public partial class FileManager : ITagRequestHandler
    {
        #region ITagRequestHandler

        IEnumerable<TagData> ITagRequestHandler.GetTagData(BaseData data)
        {
            if (data is Artist artist)
            {
                return new TagData[]
                {
                    new TagDataString(artist.Name, MusicRecord.ArtistName, ID3TagType.Performer, 0)
                };
            }
            else if (data is Album album)
            {
                return new TagData[]
                {
                    new TagDataString(album.Title, MusicRecord.AlbumTitle, ID3TagType.Album),
                    new TagDataInt(album.Year, MusicRecord.AlbumYear, ID3TagType.Year)
                };
            }
            else if (data is Song song)
            {
                return new TagData[]
                {
                    new TagDataString(song.Title, MusicRecord.SongTitle)
                };
            }
            else if (data is Recording recording)
            {
                return new TagData[]
                {
                    new TagViewable(recording.Filename, MusicRecord.Filename),
                    new TagDataString(recording.Artist.Name, MusicRecord.ArtistName, ID3TagType.Performer, 0),
                    new TagDataString(recording.Song.Title, MusicRecord.SongTitle),
                    new TagDataString(recording.Title, MusicRecord.TrackTitle, ID3TagType.Title),
                    new TagDataString(recording.Album.Title, MusicRecord.AlbumTitle, ID3TagType.Album),
                    new TagDataInt(recording.Album.Year, MusicRecord.AlbumYear, ID3TagType.Year),
                    new TagDataInt(recording.TrackNumber, MusicRecord.TrackNumber, ID3TagType.Track),
                    new TagDataInt(recording.DiscNumber, MusicRecord.DiscNumber, ID3TagType.Disc),
                    new TagDataBool(recording.Live, MusicRecord.Live),
                };
            }

            Console.WriteLine($"Error: Operation not defined for data: {data}");
            return null;
        }


        IEnumerable<TagData> ITagRequestHandler.GetTagData(IEnumerable<BaseData> data)
        {
            BaseData firstDatum = data.First();

            if (firstDatum is Artist artist)
            {
                return new TagData[]
                {
                    new TagDataString(artist.Name, MusicRecord.ArtistName, ID3TagType.Performer, 0)
                };
            }
            else if (firstDatum is Album album)
            {
                return new TagData[]
                {
                    new TagDataString(album.Title, MusicRecord.AlbumTitle, ID3TagType.Album),
                    new TagDataInt(album.Year, MusicRecord.AlbumYear, ID3TagType.Year)
                };
            }
            else if (firstDatum is Song song)
            {
                return new TagData[]
                {
                    new TagDataString(song.Title, MusicRecord.SongTitle)
                };
            }
            else if (firstDatum is Recording recording)
            {
                return new TagData[]
                {
                    new TagViewable(recording.Filename, MusicRecord.Filename),
                    new TagDataString(recording.Artist.Name, MusicRecord.ArtistName, ID3TagType.Performer, 0),
                    new TagDataString(recording.Song.Title, MusicRecord.SongTitle),
                    new TagDataString(recording.Title, MusicRecord.TrackTitle, ID3TagType.Title),
                    new TagDataString(recording.Album.Title, MusicRecord.AlbumTitle, ID3TagType.Album),
                    new TagDataInt(recording.Album.Year, MusicRecord.AlbumYear, ID3TagType.Year),
                    new TagDataInt(recording.TrackNumber, MusicRecord.TrackNumber, ID3TagType.Track),
                    new TagDataInt(recording.DiscNumber, MusicRecord.DiscNumber, ID3TagType.Disc),
                    new TagDataBool(recording.Live, MusicRecord.Live),
                };
            }

            Console.WriteLine($"Error: Operation not defined for data: {data}");
            return null;
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
                return album.Recordings.Select(x => x.Filename);
            }
            else if (data is Song song)
            {
                return song.Recordings.Distinct().Select(x => x.Filename);
            }
            else if (data is Recording recording)
            {
                return new string[] { recording.Filename };
            }

            Console.WriteLine($"Error: Operation not defined for data: {data}");
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
                return data.Cast<Artist>().SelectMany(x => x.Recordings).Distinct().Select(x => x.Filename);
            }
            else if (firstDatum is Album)
            {
                return data.Cast<Album>().SelectMany(x => x.Recordings).Select(x => x.Filename);
            }
            else if (firstDatum is Song)
            {
                return data.Cast<Song>().SelectMany(x => x.Recordings).Distinct().Select(x => x.Filename);
            }
            else if (firstDatum is Recording)
            {
                return data.Cast<Recording>().Select(x => x.Filename);
            }

            Console.WriteLine($"Error: Operation not defined for data: {firstDatum}");
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
                    if (firstDatum is Song)
                    {
                        //Renaming (or Consolidating) Songs
                        songCommands.UpdateSongTitle(data.Cast<Song>(), newString);
                    }
                    else if (firstDatum is Recording)
                    {
                        //Splitting, Renaming, And/Or Consolidating Tracks by Song Title
                        recordingCommands.UpdateSongTitle(data.Cast<Recording>(), newString);
                    }
                    else
                    {
                        throw new LibraryContextException(
                            $"Bad Context ({firstDatum}) for RecordUpdate ({record.ToString()})");
                    }
                    break;

                case MusicRecord.ArtistName:
                    if (firstDatum is Artist)
                    {
                        //Renaming and collapsing Artists
                        artistCommands.UpdateArtistName(data.Cast<Artist>(), newString);
                    }
                    else if (firstDatum is Recording)
                    {
                        //Assinging Songs to a different artist
                        recordingCommands.UpdateArtistName(data.Cast<Recording>(), newString);
                    }
                    else
                    {
                        throw new LibraryContextException(
                            $"Bad Context ({firstDatum}) for RecordUpdate ({record.ToString()})");
                    }
                    break;

                case MusicRecord.AlbumTitle:
                    if (firstDatum is Album)
                    {
                        //Renaming and collapsing Albums
                        albumCommands.UpdateAlbumTitle(data.Cast<Album>(), newString);
                    }
                    else if (firstDatum is Recording)
                    {
                        //Assigning a track to a different album
                        recordingCommands.UpdateAlbumTitle(data.Cast<Recording>(), newString);
                    }
                    else
                    {
                        throw new LibraryContextException(
                            $"Bad Context ({firstDatum}) for RecordUpdate ({record.ToString()})");
                    }
                    break;

                case MusicRecord.TrackTitle:
                    if (firstDatum is Recording)
                    {
                        //Renaming and collapsing Albums
                        recordingCommands.UpdateRecordingTitle(data.Cast<Recording>(), newString);
                    }
                    else
                    {
                        throw new LibraryContextException(
                            $"Bad Context ({firstDatum}) for RecordUpdate ({record.ToString()})");
                    }
                    break;

                default:
                    throw new Exception(
                        $"Wrong field type submitted. Submitted {newString.GetType().ToString()} for " +
                        $"field {record.ToString()}.");
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
                    if (firstDatum is Recording)
                    {
                        //Updating the track number of a track
                        recordingCommands.UpdateTrackNumber(data.Cast<Recording>(), newInt);
                    }
                    else
                    {
                        throw new LibraryContextException(
                            $"Bad Context ({firstDatum}) for RecordUpdate ({record.ToString()})");
                    }
                    break;

                case MusicRecord.AlbumYear:
                    if (firstDatum is Album)
                    {
                        //Updating the year that an album was produced
                        albumCommands.UpdateYear(data.Cast<Album>(), newInt);
                    }
                    else if (firstDatum is Recording)
                    {
                        //Updating the year that an album was produced
                        recordingCommands.UpdateYear(data.Cast<Recording>(), newInt);
                    }
                    else
                    {
                        throw new LibraryContextException(
                            $"Bad Context ({firstDatum}) for RecordUpdate ({record.ToString()})");
                    }
                    break;

                case MusicRecord.DiscNumber:
                    if (firstDatum is Recording)
                    {
                        //Updating the disc that a track appeared on
                        recordingCommands.UpdateDiscNumber(data.Cast<Recording>(), newInt);
                    }
                    else
                    {
                        throw new LibraryContextException(
                            $"Bad Context ({firstDatum}) for RecordUpdate ({record.ToString()})");
                    }
                    break;

                default:
                    throw new Exception(
                        $"Wrong field type submitted. Submitted {newInt.GetType().ToString()} for " +
                        $"field {record.ToString()}.");
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
                    if (firstDatum is Recording)
                    {
                        //Update Recording Live Status
                        recordingCommands.UpdateLive(data.Cast<Recording>(), newBool);
                    }
                    else
                    {
                        throw new LibraryContextException(
                            $"Bad Context ({firstDatum}) for RecordUpdate ({record.ToString()})");
                    }
                    break;

                default:
                    throw new Exception(
                        $"Wrong field type submitted. Submitted {newBool.GetType().ToString()} for " +
                        $"field {record.ToString()}.");
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
