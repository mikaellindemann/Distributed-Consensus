using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.DTO.Event;

namespace Common.DTO.Shared
{
    public class ServerEventDto : EventDto
    {
        [Required]
        public Uri Uri { get; set; }
    }
}
