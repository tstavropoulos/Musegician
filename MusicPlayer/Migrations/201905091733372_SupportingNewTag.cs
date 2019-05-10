namespace Musegician.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SupportingNewTag : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Albums", "AlbumGuidTimestamp", c => c.Long(nullable: false));
            AddColumn("dbo.Recordings", "RecordingType", c => c.Int(nullable: false));
            AddColumn("dbo.Artists", "ArtistGuidTimestamp", c => c.Long(nullable: false));
            AddColumn("dbo.Songs", "SongGuidTimestamp", c => c.Long(nullable: false));

            //Convert Live to RecordingType
            Sql("UPDATE dbo.Recordings SET RecordingType = (CASE WHEN Live = 1 THEN 3 ELSE 0 END)");
        }
        
        public override void Down()
        {
            //Downconvert RecordingType to Live field
            Sql("UPDATE dbo.Recordings SET Live = (CASE WHEN RecordingType = 3 THEN 1 ELSE 0 END)");

            DropColumn("dbo.Songs", "SongGuidTimestamp");
            DropColumn("dbo.Artists", "ArtistGuidTimestamp");
            DropColumn("dbo.Recordings", "RecordingType");
            DropColumn("dbo.Albums", "AlbumGuidTimestamp");
        }
    }
}
