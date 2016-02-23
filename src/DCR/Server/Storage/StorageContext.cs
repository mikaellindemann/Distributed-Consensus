using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using Common.DTO.History;
using Server.Interfaces;
using Server.Models;

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
        }

        public DbSet<ServerEventModel> Events { get; set; }
        public DbSet<ServerWorkflowModel> Workflows { get; set; }
        public DbSet<ServerUserModel> Users { get; set; }
        public DbSet<ServerRoleModel> Roles { get; set; }
        public DbSet<HistoryModel> History { get; set; }
    }
}