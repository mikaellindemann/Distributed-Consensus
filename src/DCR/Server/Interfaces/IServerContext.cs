using System;
using System.Data.Entity;
using System.Threading.Tasks;
using Server.Models;
using Server.Models.UriClasses;

namespace Server.Interfaces
{
    public interface IServerContext : IDisposable
    {
        DbSet<ServerEventModel> Events { get; set; }
        DbSet<ServerWorkflowModel> Workflows { get; set; }
        DbSet<ServerUserModel> Users { get; set; }
        DbSet<ServerRoleModel> Roles { get; set; }
        DbSet<ActionModel> History { get; set; }
        DbSet<ConditionUri> Conditions { get; set; }
        DbSet<ResponseUri> Responses { get; set; }
        DbSet<InclusionUri> Inclusions { get; set; }
        DbSet<ExclusionUri> Exclusions { get; set; }
        DbSet<MilestoneUri> Milestones { get; set; }
        int SaveChanges();
        Task<int> SaveChangesAsync();
    }
}
