namespace Musegician.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingGuids : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Albums", "AlbumGuid", c => c.Guid(nullable: false, defaultValue: Guid.NewGuid()));
            AddColumn("dbo.Artists", "ArtistGuid", c => c.Guid(nullable: false, defaultValue: Guid.NewGuid()));
            AddColumn("dbo.Songs", "SongGuid", c => c.Guid(nullable: false, defaultValue: Guid.NewGuid()));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Songs", "SongGuid");
            DropColumn("dbo.Artists", "ArtistGuid");
            DropColumn("dbo.Albums", "AlbumGuid");
        }
    }
}
