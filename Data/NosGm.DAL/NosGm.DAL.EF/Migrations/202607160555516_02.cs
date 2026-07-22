namespace NosGm.DAL.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _02 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Quest", "LevelMax", c => c.Int(nullable: false));
            AlterColumn("dbo.Quest", "LevelMin", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Quest", "LevelMin", c => c.Byte(nullable: false));
            AlterColumn("dbo.Quest", "LevelMax", c => c.Byte(nullable: false));
        }
    }
}
