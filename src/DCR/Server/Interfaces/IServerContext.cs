using System;
using System.Data.Entity;
using System.Threading.Tasks;
using Common.DTO.History;
using Server.Models;

namespace Server.Interfaces
{
    public interface IServerContext : IDisposable
    {
        DbSet<ServerEventModel> Events { get; set; }
        DbSet<ServerWorkflowModel> Workflows { get; set; }
        DbSet<ServerUserModel> Users { get; set; }
        DbSet<ServerRoleModel> Roles { get; set; }
        DbSet<HistoryModel> History { get; set; }
        int SaveChanges();
        Task<int> SaveChangesAsync();
    }
}
