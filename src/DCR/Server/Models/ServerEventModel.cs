using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Server.Models.UriClasses;

namespace Server.Models
{
    public class ServerEventModel
    {
        [Required, Key, Column(Order = 1)]
        public string Id { get; set; }

        [Required]
        public string Uri { get; set; }

        [Required, Key, Column(Order = 0)]
        public string ServerWorkflowModelId { get; set; }

        [Required]
        public virtual ServerWorkflowModel ServerWorkflowModel { get; set; }

        public virtual ICollection<ServerRoleModel> ServerRolesModels { get; set; }

        // DCR Rules
        public virtual ICollection<ResponseUri> ResponseUris { get; set; }
        public virtual ICollection<InclusionUri> InclusionUris { get; set; }
        public virtual ICollection<ExclusionUri> ExclusionUris { get; set; }
        public virtual ICollection<ConditionUri> ConditionUris { get; set; }
        public virtual ICollection<MilestoneUri> MilestoneUris { get; set; }
        public bool InitialPending { get; set; }
        public bool InitialExecuted { get; set; }
        public bool InitialIncluded { get; set; }
        
    }
}