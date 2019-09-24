namespace Musegician.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemovingLiveBool : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Recordings", "Live");

            long posixTime = Core.Epoch.Time;

            Sql($"UPDATE dbo.Albums SET AlbumGuidTimestamp = {posixTime}");
            Sql($"UPDATE dbo.Artists SET ArtistGuidTimestamp = {posixTime}");
            Sql($"UPDATE dbo.Songs SET SongGuidTimestamp = {posixTime}");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Recordings", "Live", c => c.Boolean(nullable: false));
        }
    }
}
