namespace Musegician.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Albums",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Title = c.String(),
                        Year = c.Int(nullable: false),
                        Weight = c.Double(nullable: false),
                        Image = c.Binary(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Tracks",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        AlbumId = c.Long(nullable: false),
                        RecordingId = c.Long(nullable: false),
                        Title = c.String(),
                        TrackNumber = c.Int(nullable: false),
                        DiscNumber = c.Int(nullable: false),
                        Weight = c.Double(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Albums", t => t.AlbumId, cascadeDelete: true)
                .ForeignKey("dbo.Recordings", t => t.RecordingId, cascadeDelete: true)
                .Index(t => t.AlbumId)
                .Index(t => t.RecordingId);
            
            CreateTable(
                "dbo.Recordings",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        ArtistId = c.Long(nullable: false),
                        SongId = c.Long(nullable: false),
                        Filename = c.String(),
                        Live = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Artists", t => t.ArtistId, cascadeDelete: true)
                .ForeignKey("dbo.Songs", t => t.SongId, cascadeDelete: true)
                .Index(t => t.ArtistId)
                .Index(t => t.SongId);
            
            CreateTable(
                "dbo.Artists",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Name = c.String(),
                        Weight = c.Double(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Songs",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Title = c.String(),
                        Weight = c.Double(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.PlaylistRecordings",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        PlaylistSongId = c.Long(nullable: false),
                        RecordingId = c.Long(nullable: false),
                        Weight = c.Double(nullable: false),
                        Title = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.PlaylistSongs", t => t.PlaylistSongId, cascadeDelete: true)
                .ForeignKey("dbo.Recordings", t => t.RecordingId, cascadeDelete: true)
                .Index(t => t.PlaylistSongId)
                .Index(t => t.RecordingId);
            
            CreateTable(
                "dbo.PlaylistSongs",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        PlaylistId = c.Long(nullable: false),
                        SongId = c.Long(nullable: false),
                        Title = c.String(),
                        Number = c.Int(nullable: false),
                        Weight = c.Double(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Playlists", t => t.PlaylistId, cascadeDelete: true)
                .ForeignKey("dbo.Songs", t => t.SongId)
                .Index(t => t.PlaylistId)
                .Index(t => t.SongId);
            
            CreateTable(
                "dbo.Playlists",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Title = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PlaylistRecordings", "RecordingId", "dbo.Recordings");
            DropForeignKey("dbo.PlaylistSongs", "SongId", "dbo.Songs");
            DropForeignKey("dbo.PlaylistRecordings", "PlaylistSongId", "dbo.PlaylistSongs");
            DropForeignKey("dbo.PlaylistSongs", "PlaylistId", "dbo.Playlists");
            DropForeignKey("dbo.Tracks", "RecordingId", "dbo.Recordings");
            DropForeignKey("dbo.Recordings", "SongId", "dbo.Songs");
            DropForeignKey("dbo.Recordings", "ArtistId", "dbo.Artists");
            DropForeignKey("dbo.Tracks", "AlbumId", "dbo.Albums");
            DropIndex("dbo.PlaylistSongs", new[] { "SongId" });
            DropIndex("dbo.PlaylistSongs", new[] { "PlaylistId" });
            DropIndex("dbo.PlaylistRecordings", new[] { "RecordingId" });
            DropIndex("dbo.PlaylistRecordings", new[] { "PlaylistSongId" });
            DropIndex("dbo.Recordings", new[] { "SongId" });
            DropIndex("dbo.Recordings", new[] { "ArtistId" });
            DropIndex("dbo.Tracks", new[] { "RecordingId" });
            DropIndex("dbo.Tracks", new[] { "AlbumId" });
            DropTable("dbo.Playlists");
            DropTable("dbo.PlaylistSongs");
            DropTable("dbo.PlaylistRecordings");
            DropTable("dbo.Songs");
            DropTable("dbo.Artists");
            DropTable("dbo.Recordings");
            DropTable("dbo.Tracks");
            DropTable("dbo.Albums");
        }
    }
}
