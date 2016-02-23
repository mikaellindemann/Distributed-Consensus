using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Event
{
    public class RoleDto
    {
        [Required]
        public IEnumerable<string> Roles { get; set; }
    }
}
