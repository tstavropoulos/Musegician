namespace Musegician.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CompositeArtists : DbMigration
    {
        public override void Up()
        {
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
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.CompositeArtists", "MemberId", "dbo.Artists");
            DropForeignKey("dbo.CompositeArtists", "GroupId", "dbo.Artists");
            DropIndex("dbo.CompositeArtists", new[] { "MemberId" });
            DropIndex("dbo.CompositeArtists", new[] { "GroupId" });
            DropTable("dbo.CompositeArtists");
        }
    }
}
