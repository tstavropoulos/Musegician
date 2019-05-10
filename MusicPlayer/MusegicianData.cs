using System;
using System.Collections.Generic;
using System.Data.Entity;

using Settings = Musegician.Core.Settings;
using RecordingType = Musegician.Core.RecordingType;

namespace Musegician.Database
{
    public class MusegicianData : DbContext
    {
        public MusegicianData()
            : base("name=MusegicianData")
        {
        }

        public DbSet<Album> Albums { get; set; }
        public DbSet<Recording> Recordings { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Song> Songs { get; set; }

        public DbSet<CompositeArtist> CompositeArtists { get; set; }

        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<PlaylistSong> PlaylistSongs { get; set; }
        public DbSet<PlaylistRecording> PlaylistRecordings { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlaylistSong>()
                .HasRequired(s => s.Song)
                .WithMany()
                .WillCascadeOnDelete(false);

            //Map Artist.GroupOf through to CompositeArtist.Group
            modelBuilder.Entity<Artist>()
                .HasMany(a => a.GroupOf)
                .WithRequired(c => c.Group)
                .HasForeignKey(c => c.GroupId);

            //Map Artist.MemberOf through to CompositeArtist.Member
            modelBuilder.Entity<Artist>()
                .HasMany(a => a.MemberOf)
                .WithRequired(c => c.Member)
                .HasForeignKey(c => c.MemberId)
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
        public byte[] Thumbnail { get; set; }
        public Guid AlbumGuid { get; set; }
        public long AlbumGuidTimestamp { get; set; }

        public Album()
        {
            Recordings = new HashSet<Recording>();
        }

        public virtual ICollection<Recording> Recordings { get; set; }
    }

    public class Recording : BaseData
    {
        public long ArtistId { get; set; }
        public long AlbumId { get; set; }
        public long SongId { get; set; }
        public string Filename { get; set; }

        public string Title { get; set; }

        public int TrackNumber { get; set; }
        public int DiscNumber { get; set; }

        public RecordingType RecordingType { get; set; }

        public Recording() { }

        public virtual Album Album { get; set; }
        public virtual Artist Artist { get; set; }
        public virtual Song Song { get; set; }
        public override double DefaultWeight => Settings.Instance.GetDefaultWeight(RecordingType);

        public override double Weight { get; set; }
    }

    public class Artist : BaseData
    {
        public string Name { get; set; }
        public override double Weight { get; set; }
        public Guid ArtistGuid { get; set; }
        public long ArtistGuidTimestamp { get; set; }

        public Artist()
        {
            Recordings = new HashSet<Recording>();

            GroupOf = new HashSet<CompositeArtist>();
            MemberOf = new HashSet<CompositeArtist>();
        }

        public virtual ICollection<Recording> Recordings { get; set; }

        public virtual ICollection<CompositeArtist> GroupOf { get; set; }
        public virtual ICollection<CompositeArtist> MemberOf { get; set; }
    }

    public class Song : BaseData
    {
        public string Title { get; set; }
        public override double Weight { get; set; }
        public Guid SongGuid { get; set; }
        public long SongGuidTimestamp { get; set; }

        public Song()
        {
            Recordings = new HashSet<Recording>();
        }

        public virtual ICollection<Recording> Recordings { get; set; }
    }

    public class CompositeArtist
    {
        public long Id { get; set; }

        public long GroupId { get; set; }
        public long MemberId { get; set; }

        public virtual Artist Group { get; set; }
        public virtual Artist Member { get; set; }
    }

    public class Playlist
    {
        public long Id { get; set; }
        public string Title { get; set; }

        public Playlist()
        {
            PlaylistSongs = new HashSet<PlaylistSong>();
        }

        public virtual ICollection<PlaylistSong> PlaylistSongs { get; set; }
    }

    public class PlaylistSong : BaseData
    {
        public long PlaylistId { get; set; }
        public long SongId { get; set; }
        public string Title { get; set; }
        public int Number { get; set; }
        public override double Weight { get; set; }

        public PlaylistSong()
        {
            PlaylistRecordings = new HashSet<PlaylistRecording>();
        }

        public PlaylistSong(Song song, string title)
        {
            Song = song;
            Title = title;
            PlaylistRecordings = new HashSet<PlaylistRecording>();
        }

        public virtual Playlist Playlist { get; set; }
        public virtual Song Song { get; set; }
        public virtual ICollection<PlaylistRecording> PlaylistRecordings { get; set; }
    }

    public class PlaylistRecording : BaseData
    {
        public long PlaylistSongId { get; set; }
        public long RecordingId { get; set; }
        public override double Weight { get; set; }
        public string Title { get; set; }

        public PlaylistRecording() { }

        public PlaylistRecording(Recording recording, string title)
        {
            Recording = recording;
            Title = title;
        }

        public virtual PlaylistSong PlaylistSong { get; set; }
        public virtual Recording Recording { get; set; }
    }
}