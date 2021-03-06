﻿using System;
using System.Collections.Generic;
using System.Linq;
using Musegician.Database;

namespace Musegician.Core.DBCommands
{
    public class PlaylistCommands
    {
        private readonly MusegicianData db;

        public PlaylistCommands(MusegicianData db)
        {
            this.db = db;
        }

        #region Delete Commands

        public void _DropTable()
        {
            var allPlaylists = from playlist in db.Playlists
                               where playlist.Title != "Default"
                               select playlist;
            db.Playlists.RemoveRange(allPlaylists);

            var allPlaylistSongs = from playlistSong in db.PlaylistSongs select playlistSong;
            db.PlaylistSongs.RemoveRange(allPlaylistSongs);

            var allPlaylistRecordings = from playlistRecording in db.PlaylistRecordings select playlistRecording;
            db.PlaylistRecordings.RemoveRange(allPlaylistRecordings);
        }

        #endregion Delete Commands
    }
}
