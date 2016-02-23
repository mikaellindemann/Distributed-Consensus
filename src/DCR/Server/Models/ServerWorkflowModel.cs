using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class ServerWorkflowModel
    {
        public ServerWorkflowModel()
        {
            ServerRolesModels = new List<ServerRoleModel>();
            ServerEventModels = new List<ServerEventModel>();
        }

        [Required, Key]
        public string Id { get; set; }

        public string Name { get; set; }

        public virtual ICollection<ServerEventModel> ServerEventModels { get; set; }

        public virtual ICollection<ServerRoleModel> ServerRolesModels { get; set; }
    }
}