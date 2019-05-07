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
            else if (data is Track track)
            {
                return new TagData[]
                {
                    new TagViewable(track.Recording.Filename, MusicRecord.Filename),
                    new TagDataBool(track.Recording.Live, MusicRecord.Live),
                    new TagDataString(track.Title, MusicRecord.TrackTitle, ID3TagType.Title),
                    new TagDataString(track.Recording.Song.Title, MusicRecord.SongTitle),
                    new TagDataString(track.Album.Title, MusicRecord.AlbumTitle, ID3TagType.Album),
                    new TagDataInt(track.TrackNumber, MusicRecord.TrackNumber, ID3TagType.Track),
                    new TagDataInt(track.DiscNumber, MusicRecord.DiscNumber, ID3TagType.Disc),
                    new TagDataInt(track.Album.Year, MusicRecord.AlbumYear, ID3TagType.Year),
                    new TagDataString(track.Recording.Artist.Name, MusicRecord.ArtistName, ID3TagType.Performer, 0)
                };
            }
            else if (data is Recording recording)
            {
                return new TagData[]
                {
                    new TagViewable(recording.Filename, MusicRecord.Filename),
                    new TagDataBool(recording.Live, MusicRecord.Live),
                    new TagDataString(recording.Song.Title, MusicRecord.SongTitle),
                    new TagDataString(recording.Artist.Name, MusicRecord.ArtistName, ID3TagType.Performer, 0)
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
            else if (firstDatum is Track track)
            {
                return new TagData[]
                {
                    new TagViewable(track.Recording.Filename, MusicRecord.Filename),
                    new TagDataBool(track.Recording.Live, MusicRecord.Live),
                    new TagDataString(track.Title, MusicRecord.TrackTitle, ID3TagType.Title),
                    new TagDataString(track.Recording.Song.Title, MusicRecord.SongTitle),
                    new TagDataString(track.Album.Title, MusicRecord.AlbumTitle, ID3TagType.Album),
                    new TagDataInt(track.TrackNumber, MusicRecord.TrackNumber, ID3TagType.Track),
                    new TagDataInt(track.DiscNumber, MusicRecord.DiscNumber, ID3TagType.Disc),
                    new TagDataInt(track.Album.Year, MusicRecord.AlbumYear, ID3TagType.Year),
                    new TagDataString(track.Recording.Artist.Name, MusicRecord.ArtistName, ID3TagType.Performer, 0)
                };
            }
            else if (firstDatum is Recording recording)
            {
                return new TagData[]
                {
                    new TagViewable(recording.Filename, MusicRecord.Filename),
                    new TagDataBool(recording.Live, MusicRecord.Live),
                    new TagDataString(recording.Song.Title, MusicRecord.SongTitle),
                    new TagDataString(recording.Artist.Name, MusicRecord.ArtistName, ID3TagType.Performer, 0)
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
                        else if (firstDatum is Track)
                        {
                            //Updating the year that an album was produced
                            trackCommands.UpdateYear(data.Select(x => x as Track), newInt);
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
