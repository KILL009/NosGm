namespace NosGm.DAL.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class udpate : DbMigration
    {
        public override void Up()
        {
            DropTable("dbo.BoxItem");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.BoxItem",
                c => new
                    {
                        BoxItemId = c.Long(nullable: false, identity: true),
                        ItemGeneratedAmount = c.Short(nullable: false),
                        ItemGeneratedDesign = c.Short(nullable: false),
                        ItemGeneratedRare = c.Byte(nullable: false),
                        ItemGeneratedUpgrade = c.Byte(nullable: false),
                        ItemGeneratedVNum = c.Short(nullable: false),
                        OriginalItemDesign = c.Short(nullable: false),
                        OriginalItemVNum = c.Short(nullable: false),
                        Probability = c.Byte(nullable: false),
                    })
                .PrimaryKey(t => t.BoxItemId);
            
        }
    }
}
