namespace OpenNos.DAL.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FastTravel1 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CharacterVisitedMaps",
                c => new
                    {
                        CharacterVisitedMapId = c.Long(nullable: false, identity: true),
                        CharacterId = c.Long(nullable: false),
                        MapId = c.Int(nullable: false),
                        MapX = c.Int(nullable: false),
                        MapY = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.CharacterVisitedMapId)
                .ForeignKey("dbo.Character", t => t.CharacterId, cascadeDelete: true)
                .Index(t => t.CharacterId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.CharacterVisitedMaps", "CharacterId", "dbo.Character");
            DropIndex("dbo.CharacterVisitedMaps", new[] { "CharacterId" });
            DropTable("dbo.CharacterVisitedMaps");
        }
    }
}
