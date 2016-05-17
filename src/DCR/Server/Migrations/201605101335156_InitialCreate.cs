namespace Server.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ConditionUris",
                c => new
                    {
                        WorkflowId = c.String(nullable: false, maxLength: 128),
                        EventId = c.String(nullable: false, maxLength: 128),
                        ForeignEventId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.WorkflowId, t.EventId, t.ForeignEventId })
                .ForeignKey("dbo.ServerEventModels", t => new { t.WorkflowId, t.EventId }, cascadeDelete: true)
                .Index(t => new { t.WorkflowId, t.EventId });
            
            CreateTable(
                "dbo.ServerEventModels",
                c => new
                    {
                        ServerWorkflowModelId = c.String(nullable: false, maxLength: 128),
                        Id = c.String(nullable: false, maxLength: 128),
                        Uri = c.String(nullable: false),
                        InitialPending = c.Boolean(nullable: false),
                        InitialExecuted = c.Boolean(nullable: false),
                        InitialIncluded = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => new { t.ServerWorkflowModelId, t.Id })
                .ForeignKey("dbo.ServerWorkflowModels", t => t.ServerWorkflowModelId, cascadeDelete: true)
                .Index(t => t.ServerWorkflowModelId);
            
            CreateTable(
                "dbo.ExclusionUris",
                c => new
                    {
                        WorkflowId = c.String(nullable: false, maxLength: 128),
                        EventId = c.String(nullable: false, maxLength: 128),
                        ForeignEventId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.WorkflowId, t.EventId, t.ForeignEventId })
                .ForeignKey("dbo.ServerEventModels", t => new { t.WorkflowId, t.EventId }, cascadeDelete: true)
                .Index(t => new { t.WorkflowId, t.EventId });
            
            CreateTable(
                "dbo.InclusionUris",
                c => new
                    {
                        WorkflowId = c.String(nullable: false, maxLength: 128),
                        EventId = c.String(nullable: false, maxLength: 128),
                        ForeignEventId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.WorkflowId, t.EventId, t.ForeignEventId })
                .ForeignKey("dbo.ServerEventModels", t => new { t.WorkflowId, t.EventId }, cascadeDelete: true)
                .Index(t => new { t.WorkflowId, t.EventId });
            
            CreateTable(
                "dbo.MilestoneUris",
                c => new
                    {
                        WorkflowId = c.String(nullable: false, maxLength: 128),
                        EventId = c.String(nullable: false, maxLength: 128),
                        ForeignEventId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.WorkflowId, t.EventId, t.ForeignEventId })
                .ForeignKey("dbo.ServerEventModels", t => new { t.WorkflowId, t.EventId }, cascadeDelete: true)
                .Index(t => new { t.WorkflowId, t.EventId });
            
            CreateTable(
                "dbo.ResponseUris",
                c => new
                    {
                        WorkflowId = c.String(nullable: false, maxLength: 128),
                        EventId = c.String(nullable: false, maxLength: 128),
                        ForeignEventId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.WorkflowId, t.EventId, t.ForeignEventId })
                .ForeignKey("dbo.ServerEventModels", t => new { t.WorkflowId, t.EventId }, cascadeDelete: true)
                .Index(t => new { t.WorkflowId, t.EventId });
            
            CreateTable(
                "dbo.ServerRoleModels",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        ServerWorkflowModelId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.Id, t.ServerWorkflowModelId })
                .ForeignKey("dbo.ServerWorkflowModels", t => t.ServerWorkflowModelId, cascadeDelete: true)
                .Index(t => t.ServerWorkflowModelId);
            
            CreateTable(
                "dbo.ServerUserModels",
                c => new
                    {
                        Name = c.String(nullable: false, maxLength: 128),
                        Password = c.String(),
                    })
                .PrimaryKey(t => t.Name);
            
            CreateTable(
                "dbo.ServerWorkflowModels",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ActionModels",
                c => new
                    {
                        Timestamp = c.Int(nullable: false, identity: true),
                        EventId = c.String(),
                        WorkflowId = c.String(),
                        CounterpartId = c.String(),
                        CounterpartTimeStamp = c.Int(nullable: false),
                        Type = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Timestamp);
            
            CreateTable(
                "dbo.UserRoles",
                c => new
                    {
                        UserRefId = c.String(nullable: false, maxLength: 128),
                        RoleRefId = c.String(nullable: false, maxLength: 128),
                        WorkflowRefId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.UserRefId, t.RoleRefId, t.WorkflowRefId })
                .ForeignKey("dbo.ServerUserModels", t => t.UserRefId)
                .ForeignKey("dbo.ServerRoleModels", t => new { t.RoleRefId, t.WorkflowRefId })
                .Index(t => t.UserRefId)
                .Index(t => new { t.RoleRefId, t.WorkflowRefId });
            
            CreateTable(
                "dbo.EventRoles",
                c => new
                    {
                        WorkflowId = c.String(nullable: false, maxLength: 128),
                        EventRefId = c.String(nullable: false, maxLength: 128),
                        RoleRefId = c.String(nullable: false, maxLength: 128),
                        WorkflowRefId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.WorkflowId, t.EventRefId, t.RoleRefId, t.WorkflowRefId })
                .ForeignKey("dbo.ServerEventModels", t => new { t.WorkflowId, t.EventRefId })
                .ForeignKey("dbo.ServerRoleModels", t => new { t.RoleRefId, t.WorkflowRefId })
                .Index(t => new { t.WorkflowId, t.EventRefId })
                .Index(t => new { t.RoleRefId, t.WorkflowRefId });
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ConditionUris", new[] { "WorkflowId", "EventId" }, "dbo.ServerEventModels");
            DropForeignKey("dbo.ServerEventModels", "ServerWorkflowModelId", "dbo.ServerWorkflowModels");
            DropForeignKey("dbo.EventRoles", new[] { "RoleRefId", "WorkflowRefId" }, "dbo.ServerRoleModels");
            DropForeignKey("dbo.EventRoles", new[] { "WorkflowId", "EventRefId" }, "dbo.ServerEventModels");
            DropForeignKey("dbo.ServerRoleModels", "ServerWorkflowModelId", "dbo.ServerWorkflowModels");
            DropForeignKey("dbo.UserRoles", new[] { "RoleRefId", "WorkflowRefId" }, "dbo.ServerRoleModels");
            DropForeignKey("dbo.UserRoles", "UserRefId", "dbo.ServerUserModels");
            DropForeignKey("dbo.ResponseUris", new[] { "WorkflowId", "EventId" }, "dbo.ServerEventModels");
            DropForeignKey("dbo.MilestoneUris", new[] { "WorkflowId", "EventId" }, "dbo.ServerEventModels");
            DropForeignKey("dbo.InclusionUris", new[] { "WorkflowId", "EventId" }, "dbo.ServerEventModels");
            DropForeignKey("dbo.ExclusionUris", new[] { "WorkflowId", "EventId" }, "dbo.ServerEventModels");
            DropIndex("dbo.EventRoles", new[] { "RoleRefId", "WorkflowRefId" });
            DropIndex("dbo.EventRoles", new[] { "WorkflowId", "EventRefId" });
            DropIndex("dbo.UserRoles", new[] { "RoleRefId", "WorkflowRefId" });
            DropIndex("dbo.UserRoles", new[] { "UserRefId" });
            DropIndex("dbo.ServerRoleModels", new[] { "ServerWorkflowModelId" });
            DropIndex("dbo.ResponseUris", new[] { "WorkflowId", "EventId" });
            DropIndex("dbo.MilestoneUris", new[] { "WorkflowId", "EventId" });
            DropIndex("dbo.InclusionUris", new[] { "WorkflowId", "EventId" });
            DropIndex("dbo.ExclusionUris", new[] { "WorkflowId", "EventId" });
            DropIndex("dbo.ServerEventModels", new[] { "ServerWorkflowModelId" });
            DropIndex("dbo.ConditionUris", new[] { "WorkflowId", "EventId" });
            DropTable("dbo.EventRoles");
            DropTable("dbo.UserRoles");
            DropTable("dbo.ActionModels");
            DropTable("dbo.ServerWorkflowModels");
            DropTable("dbo.ServerUserModels");
            DropTable("dbo.ServerRoleModels");
            DropTable("dbo.ResponseUris");
            DropTable("dbo.MilestoneUris");
            DropTable("dbo.InclusionUris");
            DropTable("dbo.ExclusionUris");
            DropTable("dbo.ServerEventModels");
            DropTable("dbo.ConditionUris");
        }
    }
}
