using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using Common.DTO.History;
using Server.Interfaces;
using Server.Models;
using Server.Models.UriClasses;

namespace Server.Storage
{
    public class StorageContext : DbContext, IServerContext
    {
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();

            modelBuilder.Entity<ServerUserModel>()
                .HasMany(user => user.ServerRolesModels)
                .WithMany(role => role.ServerUserModels)
                .Map(m => m
                    .MapLeftKey("UserRefId")
                    .MapRightKey("RoleRefId", "WorkflowRefId")
                    .ToTable("UserRoles"));

            modelBuilder.Entity<ServerEventModel>()
                .HasMany(@event => @event.ServerRolesModels)
                .WithMany(role => role.ServerEventModels)
                .Map(m => m
                    .MapLeftKey("WorkflowId", "EventRefId")
                    .MapRightKey("RoleRefId", "WorkflowRefId")
                    .ToTable("EventRoles"));

            modelBuilder.Entity<ServerRoleModel>()
                .HasRequired(role => role.ServerWorkflowModel)
                .WithMany(workflow => workflow.ServerRolesModels)
                .HasForeignKey(role => role.ServerWorkflowModelId);

            modelBuilder.Entity<ConditionUri>()
                .HasRequired(c => c.Event)
                .WithMany(e => e.ConditionUris)
                .HasForeignKey(c => new { c.WorkflowId, c.EventId });

            modelBuilder.Entity<ResponseUri>()
                .HasRequired(c => c.Event)
                .WithMany(e => e.ResponseUris)
                .HasForeignKey(c => new { c.WorkflowId, c.EventId });

            modelBuilder.Entity<InclusionUri>()
                .HasRequired(c => c.Event)
                .WithMany(e => e.InclusionUris)
                .HasForeignKey(c => new { c.WorkflowId, c.EventId });

            modelBuilder.Entity<ExclusionUri>()
                .HasRequired(c => c.Event)
                .WithMany(e => e.ExclusionUris)
                .HasForeignKey(c => new { c.WorkflowId, c.EventId });
        }

        public DbSet<ServerEventModel> Events { get; set; }
        public DbSet<ServerWorkflowModel> Workflows { get; set; }
        public DbSet<ServerUserModel> Users { get; set; }
        public DbSet<ServerRoleModel> Roles { get; set; }
        public DbSet<ActionModel> History { get; set; }
        public DbSet<ConditionUri> Conditions { get; set; }
        public DbSet<ResponseUri> Responses { get; set; }
        public DbSet<InclusionUri> Inclusions { get; set; }
        public DbSet<ExclusionUri> Exclusions { get; set; }
    }
}