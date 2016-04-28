using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Event.Models.UriClasses;

namespace Event.Models
{
    public class EventModel
    {
        public EventModel()
        {
            ResponseUris = new List<ResponseUri>();
            InclusionUris = new List<InclusionUri>();
            ExclusionUris = new List<ExclusionUri>();
            ConditionUris = new List<ConditionUri>();
            MilestoneUris = new List<MilestoneUri>();
        }
        [Key, Column(Order = 0)]
        public string WorkflowId { get; set; }
        [Key, Column(Order = 1)]
        public string Id { get; set; }
        public string OwnUri { get; set; }
        public string Name { get; set; }
        public bool Executed { get; set; }
        public bool Included { get; set; }
        public bool Pending { get; set; }
        public virtual ICollection<EventRoleModel> Roles { get; set; }
        public virtual ICollection<ResponseUri> ResponseUris { get; set; }
        public virtual ICollection<InclusionUri> InclusionUris { get; set; }
        public virtual ICollection<ExclusionUri> ExclusionUris { get; set; }
        public virtual ICollection<ConditionUri> ConditionUris { get; set; }
        public virtual ICollection<MilestoneUri> MilestoneUris { get; set; }
        public string LockOwner { get; set; }
        public bool IsEvil { get; set; }
        public virtual ICollection<CheatingType> TypesOfCheating { get; set; } 

        public bool InitialPending { get; set; }
        public bool InitialExecuted { get; set; }
        public bool InitialIncluded { get; set; }
        
    }
}