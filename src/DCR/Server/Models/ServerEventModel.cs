using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    }
}