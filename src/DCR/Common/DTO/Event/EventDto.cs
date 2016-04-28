using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Common.DTO.Shared;

namespace Common.DTO.Event
{
    public class EventDto
    {
        [Required]
        public string EventId { get; set; }
        [Required]
        public string WorkflowId { get; set; }
        [Required]
        public string Name { get; set; }
        public bool Pending { get; set; }
        public bool Executed { get; set; }
        public bool Included { get; set; }
        [Required]
        public IEnumerable<string> Roles { get; set; }
        public IEnumerable<EventAddressDto> Conditions { get; set; }
        public IEnumerable<EventAddressDto> Exclusions { get; set; }
        public IEnumerable<EventAddressDto> Responses { get; set; }
        public IEnumerable<EventAddressDto> Inclusions { get; set; }
        public IEnumerable<EventAddressDto> Milestones { get; set; }
    }
}
