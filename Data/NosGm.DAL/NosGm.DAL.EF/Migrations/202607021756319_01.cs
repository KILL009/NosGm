namespace NosGm.DAL.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _01 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.NpcMonster", "NoticeRange", c => c.Short(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.NpcMonster", "NoticeRange", c => c.Byte(nullable: false));
        }
    }
}
