namespace Event.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _13042016Wind2 : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.ActionModels");
            AlterColumn("dbo.ActionModels", "Timestamp", c => c.Int(nullable: false));
            AlterColumn("dbo.ActionModels", "EventId", c => c.String(nullable: false, maxLength: 128));
            AddPrimaryKey("dbo.ActionModels", new[] { "Timestamp", "EventId" });
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.ActionModels");
            AlterColumn("dbo.ActionModels", "EventId", c => c.String());
            AlterColumn("dbo.ActionModels", "Timestamp", c => c.Int(nullable: false, identity: true));
            AddPrimaryKey("dbo.ActionModels", "Timestamp");
        }
    }
}
