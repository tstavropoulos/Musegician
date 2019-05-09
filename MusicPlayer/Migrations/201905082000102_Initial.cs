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
                        AlbumGuid = c.Guid(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Recordings",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        ArtistId = c.Long(nullable: false),
                        AlbumId = c.Long(nullable: false),
                        SongId = c.Long(nullable: false),
                        Filename = c.String(),
                        Title = c.String(),
                        TrackNumber = c.Int(nullable: false),
                        DiscNumber = c.Int(nullable: false),
                        Live = c.Boolean(nullable: false),
                        Weight = c.Double(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Albums", t => t.AlbumId, cascadeDelete: true)
                .ForeignKey("dbo.Artists", t => t.ArtistId, cascadeDelete: true)
                .ForeignKey("dbo.Songs", t => t.SongId, cascadeDelete: true)
                .Index(t => t.ArtistId)
                .Index(t => t.AlbumId)
                .Index(t => t.SongId);
            
            CreateTable(
                "dbo.Artists",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Name = c.String(),
                        Weight = c.Double(nullable: false),
                        ArtistGuid = c.Guid(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.CompositeArtists",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        GroupId = c.Long(nullable: false),
                        MemberId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Artists", t => t.GroupId, cascadeDelete: true)
                .ForeignKey("dbo.Artists", t => t.MemberId)
                .Index(t => t.GroupId)
                .Index(t => t.MemberId);
            
            CreateTable(
                "dbo.Songs",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Title = c.String(),
                        Weight = c.Double(nullable: false),
                        SongGuid = c.Guid(nullable: false),
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
            DropForeignKey("dbo.Recordings", "SongId", "dbo.Songs");
            DropForeignKey("dbo.Recordings", "ArtistId", "dbo.Artists");
            DropForeignKey("dbo.CompositeArtists", "MemberId", "dbo.Artists");
            DropForeignKey("dbo.CompositeArtists", "GroupId", "dbo.Artists");
            DropForeignKey("dbo.Recordings", "AlbumId", "dbo.Albums");

            DropIndex("dbo.PlaylistSongs", new[] { "SongId" });
            DropIndex("dbo.PlaylistSongs", new[] { "PlaylistId" });
            DropIndex("dbo.PlaylistRecordings", new[] { "RecordingId" });
            DropIndex("dbo.PlaylistRecordings", new[] { "PlaylistSongId" });
            DropIndex("dbo.CompositeArtists", new[] { "MemberId" });
            DropIndex("dbo.CompositeArtists", new[] { "GroupId" });
            DropIndex("dbo.Recordings", new[] { "SongId" });
            DropIndex("dbo.Recordings", new[] { "AlbumId" });
            DropIndex("dbo.Recordings", new[] { "ArtistId" });

            DropTable("dbo.Playlists");
            DropTable("dbo.PlaylistSongs");
            DropTable("dbo.PlaylistRecordings");
            DropTable("dbo.Songs");
            DropTable("dbo.CompositeArtists");
            DropTable("dbo.Artists");
            DropTable("dbo.Recordings");
            DropTable("dbo.Albums");
        }
    }
}
