using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace Musegician.Database
{

    public class MusegicianData : DbContext
    {
        // Your context has been configured to use a 'MusegicianData' connection string from your application's 
        // configuration file (App.config or Web.config). By default, this connection string targets the 
        // 'Musegician.MusegicianData' database on your LocalDb instance. 
        // 
        // If you wish to target a different database and/or database provider, modify the 'MusegicianData' 
        // connection string in the application configuration file.
        public MusegicianData()
            : base("name=MusegicianData")
        {
        }

        public DbSet<Album> Albums { get; set; }
        public DbSet<Recording> Recordings { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Song> Songs { get; set; }
        public DbSet<Track> Tracks { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<PlaylistSong> PlaylistSongs { get; set; }
        public DbSet<PlaylistRecording> PlaylistRecordings { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlaylistSong>()
                .HasRequired(s => s.Song)
                .WithMany()
                .WillCascadeOnDelete(false);
        }
    }

    public abstract class BaseData
    {
        public long Id { get; set; }

        public abstract double Weight { get; set; }
        public virtual double DefaultWeight => 1.0;
    }

    public class Album : BaseData
    {
        public string Title { get; set; }
        public int Year { get; set; }
        public override double Weight { get; set; }
        public byte[] Image { get; set; }
        public Guid AlbumGuid { get; set; }

        public virtual ICollection<Track> Tracks { get; set; }
    }

    public class Recording : BaseData
    {
        public long ArtistId { get; set; }
        public long SongId { get; set; }
        public string Filename { get; set; }
        public bool Live { get; set; }

        //public override double Weight
        //{
        //    get => Tracks.First().Weight;
        //    set => Tracks.First().Weight = value;
        //}

        [NotMapped]
        public override double Weight
        {
            get => throw new Exception("Recordings Have No Weight:Get");
            set => throw new Exception("Recordings Have No Weight:Set");
        }

        public virtual Artist Artist { get; set; }
        public virtual Song Song { get; set; }
        public override double DefaultWeight => Live ? Settings.Instance.LiveWeight : Settings.Instance.StudioWeight;

        public virtual ICollection<Track> Tracks { get; set; }
    }

    public class Artist : BaseData
    {
        public string Name { get; set; }
        public override double Weight { get; set; }
        public Guid ArtistGuid { get; set; }

        public virtual ICollection<Recording> Recordings { get; set; }
    }

    public class Song : BaseData
    {
        public string Title { get; set; }
        public override double Weight { get; set; }
        public Guid SongGuid { get; set; }

        public virtual ICollection<Recording> Recordings { get; set; }
    }

    public class Track : BaseData
    {
        public long AlbumId { get; set; }
        public long RecordingId { get; set; }
        public string Title { get; set; }
        public int TrackNumber { get; set; }
        public int DiscNumber { get; set; }
        public override double Weight { get; set; }

        public virtual Album Album { get; set; }
        public virtual Recording Recording { get; set; }
    }

    public class Playlist
    {
        public long Id { get; set; }
        public string Title { get; set; }

        public virtual ICollection<PlaylistSong> PlaylistSongs { get; set; }
    }

    public class PlaylistSong : BaseData
    {
        public long PlaylistId { get; set; }
        public long SongId { get; set; }
        public string Title { get; set; }
        public int Number { get; set; }
        public override double Weight { get; set; }

        public virtual Playlist Playlist { get; set; }
        public virtual Song Song { get; set; }
        public virtual ICollection<PlaylistRecording> PlaylistRecordings { get; set; }

        public PlaylistSong() { }
        public PlaylistSong(Song song, string title)
        {
            Song = song;
            Title = title;
        }
    }

    public class PlaylistRecording : BaseData
    {
        public long PlaylistSongId { get; set; }
        public long RecordingId { get; set; }
        public override double Weight { get; set; }
        public string Title { get; set; }

        public virtual PlaylistSong PlaylistSong { get; set; }
        public virtual Recording Recording { get; set; }

        public PlaylistRecording() { }
        public PlaylistRecording(Recording recording, string title)
        {
            Recording = recording;
            Title = title;
        }
    }
}