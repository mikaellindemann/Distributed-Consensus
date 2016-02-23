using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Common.DTO.Server;

namespace Common.DTO.Event
{
    public class UserDto
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public IEnumerable<WorkflowRole> Roles { get; set; }
    }
}
