using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Threading.Tasks;
using Common.DTO.History;
using Event.Models;
using Event.Models.UriClasses;

namespace Event.Interfaces
{
    /// <summary>
    /// This interface represents the properties we want to be able to manipulate in the database.
    /// The primary reason for using this interface is for unit-testing purposes and mocking. 
    /// </summary>
    public interface IEventContext : IDisposable
    {
        DbSet<EventModel> Events { get; set; }
        DbSet<ConditionUri> Conditions { get; set; }
        DbSet<ResponseUri> Responses { get; set; }
        DbSet<InclusionUri> Inclusions { get; set; }
        DbSet<ExclusionUri> Exclusions { get; set; }
        DbSet<ActionModel> History { get; set; }
        DbEntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class; 

        int SaveChanges();
        Task<int> SaveChangesAsync();
    }
}
