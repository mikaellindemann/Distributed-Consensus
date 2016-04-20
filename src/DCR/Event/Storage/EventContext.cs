using System.Data.Entity;
using Common.DTO.History;
using Event.Interfaces;
using Event.Models;
using Event.Models.UriClasses;
using ActionModel = Event.Models.ActionModel;

namespace Event.Storage
{
    public class EventContext : DbContext, IEventContext
    {
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EventModel>()
                .HasMany(e => e.Roles)
                .WithRequired(role => role.Event)
                .HasForeignKey(role => new { role.WorkflowId, role.EventId });

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

            modelBuilder.Entity<ActionModel>()
                .HasKey(model => new {model.Timestamp, model.EventId});
        }

        public DbSet<EventModel> Events { get; set; }
        public DbSet<ConditionUri> Conditions { get; set; }
        public DbSet<ResponseUri> Responses { get; set; }
        public DbSet<InclusionUri> Inclusions { get; set; }
        public DbSet<ExclusionUri> Exclusions { get; set; }
        public DbSet<ActionModel> History { get; set; }
    }
}