using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Shared
{
    public class EventAddressDto
    {
        [Required]
        public string WorkflowId { get; set; }

        [Required]
        public string Id { get; set; }

        [Required]
        public Uri Uri { get; set; }

        public IEnumerable<string> Roles { get; set; } 
    }
}
