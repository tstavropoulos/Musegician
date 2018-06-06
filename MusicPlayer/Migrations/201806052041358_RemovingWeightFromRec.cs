namespace Musegician.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemovingWeightFromRec : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Recordings", "Weight");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Recordings", "Weight", c => c.Double(nullable: false));
        }
    }
}
