namespace OpenNos.DAL.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class hasSkinSPS : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ItemInstance", "HaveSkin", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ItemInstance", "HaveSkin");
        }
    }
}
