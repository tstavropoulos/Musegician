using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using DbType = System.Data.DbType;
using Musegician.DataStructures;
using Musegician.Database;

namespace Musegician.Core.DBCommands
{
    public class PlaylistCommands
    {
        MusegicianData db = null;

        public PlaylistCommands()
        {
        }

        public void Initialize(
            MusegicianData db)
        {
            this.db = db;
        }
        
        #region Delete Commands

        public void _DropTable()
        {
            var allPlaylists = from playlist in db.Playlists select playlist;
            db.Playlists.RemoveRange(allPlaylists);

            var allPlaylistSongs = from playlistSong in db.PlaylistSongs select playlistSong;
            db.PlaylistSongs.RemoveRange(allPlaylistSongs);

            var allPlaylistRecordings = from playlistRecording in db.PlaylistRecordings select playlistRecording;
            db.PlaylistRecordings.RemoveRange(allPlaylistRecordings);
        }

        #endregion Delete Commands
    }
}
