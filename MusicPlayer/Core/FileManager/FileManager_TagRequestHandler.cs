using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SQLite;
using System.Text;
using System.Threading.Tasks;
using Musegician.DataStructures;
using Musegician.TagEditor;

using LibraryContext = Musegician.Library.LibraryContext;

namespace Musegician
{
    public partial class FileManager : ITagRequestHandler
    {
        #region ITagRequestHandler

        IEnumerable<TagData> ITagRequestHandler.GetTagData(LibraryContext context, long id)
        {
            List<TagData> tagList = new List<TagData>();

            dbConnection.Open();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.Parameters.Add(new SQLiteParameter("@ID", id));

            switch (context)
            {
                case LibraryContext.Artist:
                    {
                        readTracks.CommandText =
                            "SELECT name AS artist_name " +
                            "FROM artist " +
                            "WHERE id=@ID;";
                    }
                    break;
                case LibraryContext.Album:
                    {
                        readTracks.CommandText =
                            "SELECT title AS album_title, year " +
                            "FROM album " +
                            "WHERE id=@ID;";
                    }
                    break;
                case LibraryContext.Song:
                    {
                        readTracks.CommandText =
                            "SELECT title AS song_title " +
                            "FROM song " +
                            "WHERE id=@ID;";
                    }
                    break;
                case LibraryContext.Track:
                    {
                        readTracks.CommandText =
                            "SELECT " +
                                "song.title AS song_title, " +
                                "artist.name AS artist_name, " +
                                "album.title AS album_title, " +
                                "album.year AS year, " +
                                "track.title AS track_title, " +
                                "track.track_number AS track_number, " +
                                "track.disc_number AS disc_number," +
                                "recording.filename AS filename, " +
                                "recording.live AS live " +
                            "FROM track " +
                            "LEFT JOIN recording ON track.recording_id=recording.id " +
                            "LEFT JOIN song ON recording.song_id=song.id " +
                            "LEFT JOIN artist ON recording.artist_id=artist.id " +
                            "LEFT JOIN album ON track.album_id=album.id " +
                            "WHERE track.id=@ID;";
                    }
                    break;
                case LibraryContext.Recording:
                    {
                        readTracks.CommandText =
                            "SELECT " +
                                "song.title AS song_title, " +
                                "artist.name AS artist_name, " +
                                "recording.filename AS filename, " +
                                "recording.live AS live " +
                            "FROM recording " +
                            "LEFT JOIN song ON recording.song_id=song.id " +
                            "LEFT JOIN artist ON recording.artist_id=artist.id " +
                            "WHERE recording.id=@ID;";
                    }
                    break;
                case LibraryContext.MAX:
                default:
                    dbConnection.Close();
                    throw new Exception("Unexpected LibraryContext: " + context);
            }

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                if (reader.Read())
                {
                    if (context == LibraryContext.Track ||
                        context == LibraryContext.Recording)
                    {
                        tagList.Add(new TagViewable()
                        {
                            _CurrentValue = (string)reader["filename"],
                            recordType = MusicRecord.Filename
                        });

                        tagList.Add(new TagDataBool()
                        {
                            _currentValue = (bool)reader["live"],
                            NewValue = (bool)reader["live"],
                            recordType = MusicRecord.Live
                        });
                    }

                    if (context == LibraryContext.Track)
                    {
                        tagList.Add(new TagDataString
                        {
                            _currentValue = (string)reader["track_title"],
                            NewValue = (string)reader["track_title"],
                            recordType = MusicRecord.TrackTitle,
                            tagType = ID3TagType.Title
                        });

                        tagList.Add(new TagDataLong
                        {
                            _currentValue = (long)reader["track_number"],
                            _newValue = (long)reader["track_number"],
                            recordType = MusicRecord.TrackNumber,
                            tagType = ID3TagType.Track
                        });

                        tagList.Add(new TagDataLong
                        {
                            _currentValue = (long)reader["disc_number"],
                            _newValue = (long)reader["disc_number"],
                            recordType = MusicRecord.DiscNumber,
                            tagType = ID3TagType.Disc
                        });
                    }

                    if (context == LibraryContext.Song ||
                        context == LibraryContext.Track ||
                        context == LibraryContext.Recording)
                    {
                        tagList.Add(new TagDataString
                        {
                            _currentValue = (string)reader["song_title"],
                            NewValue = (string)reader["song_title"],
                            recordType = MusicRecord.SongTitle
                        });
                    }

                    if (context == LibraryContext.Album ||
                        context == LibraryContext.Track)
                    {
                        tagList.Add(new TagDataString
                        {
                            _currentValue = (string)reader["album_title"],
                            NewValue = (string)reader["album_title"],
                            recordType = MusicRecord.AlbumTitle,
                            tagType = ID3TagType.Album
                        });

                        tagList.Add(new TagDataLong
                        {
                            _currentValue = (long)reader["year"],
                            _newValue = (long)reader["year"],
                            recordType = MusicRecord.AlbumYear,
                            tagType = ID3TagType.Year
                        });
                    }

                    if (context == LibraryContext.Artist ||
                        context == LibraryContext.Track ||
                        context == LibraryContext.Recording)
                    {
                        tagList.Add(new TagDataString
                        {
                            _currentValue = (string)reader["artist_name"],
                            NewValue = (string)reader["artist_name"],
                            recordType = MusicRecord.ArtistName,
                            tagType = ID3TagType.Performer,
                            tagTypeIndex = 0
                        });
                    }
                }
            }

            dbConnection.Close();

            return tagList;
        }


        IEnumerable<TagData> ITagRequestHandler.GetTagData(
            LibraryContext context,
            IEnumerable<long> ids)
        {
            List<TagData> tagList = new List<TagData>();

            dbConnection.Open();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.Parameters.Add(new SQLiteParameter("@ID", ids.First()));

            switch (context)
            {
                case LibraryContext.Artist:
                    {
                        readTracks.CommandText =
                            "SELECT name AS artist_name " +
                            "FROM artist " +
                            "WHERE id=@ID;";
                    }
                    break;
                case LibraryContext.Album:
                    {
                        readTracks.CommandText =
                            "SELECT title AS album_title, year " +
                            "FROM album " +
                            "WHERE id=@ID;";
                    }
                    break;
                case LibraryContext.Song:
                    {
                        readTracks.CommandText =
                            "SELECT title AS song_title " +
                            "FROM song " +
                            "WHERE id=@ID;";
                    }
                    break;
                case LibraryContext.Track:
                    {
                        readTracks.CommandText =
                            "SELECT " +
                                "song.title AS song_title, " +
                                "artist.name AS artist_name, " +
                                "album.title AS album_title, " +
                                "album.year AS year, " +
                                "track.title AS track_title, " +
                                "track.track_number AS track_number, " +
                                "track.disc_number AS disc_number," +
                                "recording.filename AS filename, " +
                                "recording.live AS live " +
                            "FROM track " +
                            "LEFT JOIN recording ON track.recording_id=recording.id " +
                            "LEFT JOIN song ON recording.song_id=song.id " +
                            "LEFT JOIN artist ON recording.artist_id=artist.id " +
                            "LEFT JOIN album ON track.album_id=album.id " +
                            "WHERE track.id=@ID;";
                    }
                    break;
                case LibraryContext.Recording:
                    {
                        readTracks.CommandText =
                            "SELECT " +
                                "song.title AS song_title, " +
                                "artist.name AS artist_name, " +
                                "recording.filename AS filename, " +
                                "recording.live AS live " +
                            "FROM recording " +
                            "LEFT JOIN song ON recording.song_id=song.id " +
                            "LEFT JOIN artist ON recording.artist_id=artist.id " +
                            "WHERE recording.id=@ID;";
                    }
                    break;
                case LibraryContext.MAX:
                default:
                    dbConnection.Close();
                    throw new Exception("Unexpected LibraryContext: " + context);
            }

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                if (reader.Read())
                {
                    if (context == LibraryContext.Track ||
                        context == LibraryContext.Recording)
                    {
                        tagList.Add(new TagViewable()
                        {
                            _CurrentValue = (string)reader["filename"],
                            recordType = MusicRecord.Filename
                        });

                        tagList.Add(new TagDataBool()
                        {
                            _currentValue = (bool)reader["live"],
                            NewValue = (bool)reader["live"],
                            recordType = MusicRecord.Live
                        });
                    }

                    if (context == LibraryContext.Track)
                    {
                        tagList.Add(new TagDataString
                        {
                            _currentValue = (string)reader["track_title"],
                            NewValue = (string)reader["track_title"],
                            recordType = MusicRecord.TrackTitle,
                            tagType = ID3TagType.Title
                        });

                        tagList.Add(new TagDataLong
                        {
                            _currentValue = (long)reader["track_number"],
                            _newValue = (long)reader["track_number"],
                            recordType = MusicRecord.TrackNumber,
                            tagType = ID3TagType.Track
                        });

                        tagList.Add(new TagDataLong
                        {
                            _currentValue = (long)reader["disc_number"],
                            _newValue = (long)reader["disc_number"],
                            recordType = MusicRecord.DiscNumber,
                            tagType = ID3TagType.Disc
                        });
                    }

                    if (context == LibraryContext.Song ||
                        context == LibraryContext.Track ||
                        context == LibraryContext.Recording)
                    {
                        tagList.Add(new TagDataString
                        {
                            _currentValue = (string)reader["song_title"],
                            NewValue = (string)reader["song_title"],
                            recordType = MusicRecord.SongTitle
                        });
                    }

                    if (context == LibraryContext.Album ||
                        context == LibraryContext.Track)
                    {
                        tagList.Add(new TagDataString
                        {
                            _currentValue = (string)reader["album_title"],
                            NewValue = (string)reader["album_title"],
                            recordType = MusicRecord.AlbumTitle,
                            tagType = ID3TagType.Album
                        });

                        tagList.Add(new TagDataLong
                        {
                            _currentValue = (long)reader["year"],
                            _newValue = (long)reader["year"],
                            recordType = MusicRecord.AlbumYear,
                            tagType = ID3TagType.Year
                        });
                    }

                    if (context == LibraryContext.Artist ||
                        context == LibraryContext.Track ||
                        context == LibraryContext.Recording)
                    {
                        tagList.Add(new TagDataString
                        {
                            _currentValue = (string)reader["artist_name"],
                            NewValue = (string)reader["artist_name"],
                            recordType = MusicRecord.ArtistName,
                            tagType = ID3TagType.Performer,
                            tagTypeIndex = 0
                        });
                    }
                }
            }

            dbConnection.Close();

            return tagList;
        }

        /// <summary>
        /// Identifies all of the files potentially requiring ID3 tag updates
        /// </summary>
        IEnumerable<string> ITagRequestHandler.GetAffectedFiles(
            LibraryContext context,
            long id)
        {
            List<string> affectedFiles = new List<string>();

            dbConnection.Open();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.Parameters.Add(new SQLiteParameter("@ID", id));

            switch (context)
            {
                case LibraryContext.Artist:
                    readTracks.CommandText =
                        "SELECT filename " +
                        "FROM recording " +
                        "WHERE recording.artist_id=@ID;";
                    break;
                case LibraryContext.Album:
                    readTracks.CommandText =
                        "SELECT recording.filename AS filename " +
                        "FROM track " +
                        "LEFT JOIN recording ON track.recording_id=recording.id " +
                        "WHERE track.album_id=@ID;";
                    break;
                case LibraryContext.Song:
                    readTracks.CommandText =
                        "SELECT filename " +
                        "FROM recording " +
                        "WHERE recording.song_id=@ID;";
                    break;
                case LibraryContext.Track:
                    readTracks.CommandText =
                        "SELECT recording.filename AS filename " +
                        "FROM track " +
                        "LEFT JOIN recording ON track.recording_id=recording.id " +
                        "WHERE track.id=@ID;";
                    break;
                case LibraryContext.Recording:
                    readTracks.CommandText =
                        "SELECT filename " +
                        "FROM recording " +
                        "WHERE recording.id=@ID;";
                    break;
                case LibraryContext.MAX:
                default:
                    dbConnection.Close();
                    throw new Exception("Unexpected LibraryContext: " + context);
            }

            using (SQLiteDataReader reader = readTracks.ExecuteReader())
            {
                while (reader.Read())
                {
                    affectedFiles.Add((string)reader["filename"]);
                }
            }

            dbConnection.Close();

            return affectedFiles;
        }

        /// <summary>
        /// Identifies all of the files potentially requiring ID3 tag updates
        /// </summary>
        IEnumerable<string> ITagRequestHandler.GetAffectedFiles(
            LibraryContext context,
            IEnumerable<long> ids)
        {
            List<string> affectedFiles = new List<string>();

            dbConnection.Open();

            SQLiteCommand readTracks = dbConnection.CreateCommand();
            readTracks.CommandType = System.Data.CommandType.Text;
            readTracks.Parameters.Add("@ID", System.Data.DbType.Int64);

            switch (context)
            {
                case LibraryContext.Artist:
                    readTracks.CommandText =
                        "SELECT filename " +
                        "FROM recording " +
                        "WHERE recording.artist_id=@ID;";
                    break;
                case LibraryContext.Album:
                    readTracks.CommandText =
                        "SELECT recording.filename AS filename " +
                        "FROM track " +
                        "LEFT JOIN recording ON track.recording_id=recording.id " +
                        "WHERE track.album_id=@ID;";
                    break;
                case LibraryContext.Song:
                    readTracks.CommandText =
                        "SELECT filename " +
                        "FROM recording " +
                        "WHERE recording.song_id=@ID;";
                    break;
                case LibraryContext.Track:
                    readTracks.CommandText =
                        "SELECT recording.filename AS filename " +
                        "FROM track " +
                        "LEFT JOIN recording ON track.recording_id=recording.id " +
                        "WHERE track.id=@ID;";
                    break;
                case LibraryContext.Recording:
                    readTracks.CommandText =
                        "SELECT filename " +
                        "FROM recording " +
                        "WHERE recording.id=@ID;";
                    break;
                case LibraryContext.MAX:
                default:
                    dbConnection.Close();
                    throw new Exception("Unexpected LibraryContext: " + context);
            }

            foreach (long id in ids)
            {
                readTracks.Parameters["@ID"].Value = id;
                using (SQLiteDataReader reader = readTracks.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        affectedFiles.Add((string)reader["filename"]);
                    }
                }
            }

            dbConnection.Close();

            return affectedFiles;
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
            LibraryContext context,
            IEnumerable<long> ids,
            MusicRecord record,
            string newString)
        {
            if (ids.Count() == 0)
            {
                throw new InvalidOperationException(string.Format(
                    "Found 0 records to modify for LibraryContext {0}, MusicRecord {1}",
                    context.ToString(),
                    record.ToString()));
            }

            switch (record)
            {
                case MusicRecord.SongTitle:
                    {
                        switch (context)
                        {
                            case LibraryContext.Song:
                                {
                                    //Renaming (or Consolidating) Songs
                                    songCommands.UpdateSongTitle(
                                        songIDs: ids,
                                        newTitle: newString);
                                }
                                break;
                            case LibraryContext.Track:
                                {
                                    //Splitting, Renaming, And/Or Consolidating Tracks by Song Title
                                    trackCommands.UpdateSongTitle(
                                        trackIDs: ids,
                                        newTitle: newString);
                                }
                                break;
                            case LibraryContext.Recording:
                                {
                                    //Splitting, Renaming, And/Or Consolidating Tracks by Song Title
                                    recordingCommands.UpdateSongTitle(
                                        recordingIDs: ids,
                                        newTitle: newString);
                                }
                                break;
                            default:
                                throw new LibraryContextException(string.Format(
                                    "Bad Context ({0}) for RecordUpdate ({1})",
                                    context.ToString(),
                                    record.ToString()));
                        }
                    }
                    break;
                case MusicRecord.ArtistName:
                    {
                        switch (context)
                        {
                            case LibraryContext.Artist:
                                {
                                    //Renaming and collapsing Artists
                                    artistCommands.UpdateArtistName(
                                        artistIDs: ids,
                                        newArtistName: newString);
                                }
                                break;
                            case LibraryContext.Track:
                                {
                                    //Assinging Songs to a different artist
                                    trackCommands.UpdateArtistName(
                                        trackIDs: ids,
                                        newArtistName: newString);
                                }
                                break;
                            case LibraryContext.Recording:
                                {
                                    //Assinging Songs to a different artist
                                    recordingCommands.UpdateArtistName(
                                        recordingIDs: ids,
                                        newArtistName: newString);
                                }
                                break;
                            default:
                                throw new LibraryContextException(string.Format(
                                    "Bad Context ({0}) for RecordUpdate ({1})",
                                    context.ToString(),
                                    record.ToString()));
                        }

                    }
                    break;
                case MusicRecord.AlbumTitle:
                    {
                        switch (context)
                        {
                            case LibraryContext.Album:
                                //Renaming an album
                                throw new NotImplementedException();
                                break;
                            case LibraryContext.Track:
                                //Assigning a track to a different album
                                throw new NotImplementedException();
                                break;
                            default:
                                throw new LibraryContextException(string.Format(
                                    "Bad Context ({0}) for RecordUpdate ({1})",
                                    context.ToString(),
                                    record.ToString()));
                        }
                    }
                    break;
                case MusicRecord.TrackTitle:
                    {
                        switch (context)
                        {
                            case LibraryContext.Track:
                                //Renaming a track
                                throw new NotImplementedException();
                                break;
                            default:
                                throw new LibraryContextException(string.Format(
                                    "Bad Context ({0}) for RecordUpdate ({1})",
                                    context.ToString(),
                                    record.ToString()));
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
            LibraryContext context,
            IEnumerable<long> ids,
            MusicRecord record,
            long newLong)
        {
            switch (record)
            {
                case MusicRecord.TrackNumber:
                    {
                        switch (context)
                        {
                            case LibraryContext.Track:
                                //Updating the track number of a track
                                throw new NotImplementedException();
                                break;
                            default:
                                throw new LibraryContextException(string.Format(
                                    "Bad Context ({0}) for RecordUpdate ({1})",
                                    context.ToString(),
                                    record.ToString()));
                        }
                    }
                    break;
                case MusicRecord.AlbumYear:
                    {
                        switch (context)
                        {
                            case LibraryContext.Album:
                                //Updating the year that an album was produced
                                throw new NotImplementedException();
                                break;
                            default:
                                throw new LibraryContextException(string.Format(
                                    "Bad Context ({0}) for RecordUpdate ({1})",
                                    context.ToString(),
                                    record.ToString()));
                        }
                    }
                    break;
                default:
                    throw new Exception(string.Format(
                        "Wrong field type submitted. Submitted {0} for field {1}.",
                        newLong.GetType().ToString(),
                        record.ToString()));
            }
        }

        void ITagRequestHandler.UpdateRecord(
            LibraryContext context,
            IEnumerable<long> ids,
            MusicRecord record,
            bool newBool)
        {
            switch (record)
            {
                case MusicRecord.Live:
                    {
                        //Update Recording Live Status Weight
                        if (context != LibraryContext.Recording)
                        {
                            throw new LibraryContextException(string.Format(
                                "Bad Context ({0}) for RecordUpdate ({1})",
                                context.ToString(),
                                record.ToString()));
                        }
                        throw new NotImplementedException();
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
            LibraryContext context,
            IEnumerable<long> ids,
            MusicRecord record,
            double newDouble)
        {
            switch (record)
            {
                case MusicRecord.ArtistWeight:
                    {
                        //Update Artist Weight
                        if (context != LibraryContext.Artist)
                        {
                            throw new LibraryContextException(string.Format(
                                "Bad Context ({0}) for RecordUpdate ({1})",
                                context.ToString(),
                                record.ToString()));
                        }
                        throw new NotImplementedException();
                    }
                    break;
                case MusicRecord.AlbumWeight:
                    {
                        //Update Album Weight
                        if (context != LibraryContext.Album)
                        {
                            throw new LibraryContextException(string.Format(
                                "Bad Context ({0}) for RecordUpdate ({1})",
                                context.ToString(),
                                record.ToString()));
                        }
                        throw new NotImplementedException();
                    }
                    break;
                case MusicRecord.SongWeight:
                    {
                        //Update Album Weight
                        if (context != LibraryContext.Song)
                        {
                            throw new LibraryContextException(string.Format(
                                "Bad Context ({0}) for RecordUpdate ({1})",
                                context.ToString(),
                                record.ToString()));
                        }
                        throw new NotImplementedException();
                    }
                    break;
                case MusicRecord.TrackWeight:
                    {
                        //Update Track Weight
                        if (context != LibraryContext.Track)
                        {
                            throw new LibraryContextException(string.Format(
                                "Bad Context ({0}) for RecordUpdate ({1})",
                                context.ToString(),
                                record.ToString()));
                        }
                        throw new NotImplementedException();
                    }
                    break;
                default:
                    throw new Exception(string.Format(
                        "Wrong field type submitted. Submitted {0} for field {1}.",
                        newDouble.GetType().ToString(),
                        record.ToString()));
            }
        }

        #endregion ITagRequestHandler
    }
}
