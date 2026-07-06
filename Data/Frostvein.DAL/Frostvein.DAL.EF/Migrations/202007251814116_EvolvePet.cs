namespace OpenNos.DAL.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class EvolvePet : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.NpcMonster", "EvolvePet", c => c.Short(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.NpcMonster", "EvolvePet");
        }
    }
}
