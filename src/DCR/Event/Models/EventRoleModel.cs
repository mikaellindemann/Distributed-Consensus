using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Event.Models
{
    public class EventRoleModel
    {
        [Key, Column(Order = 0)]
        public string WorkflowId { get; set; }
        [Key, Column(Order = 1)]
        public string EventId { get; set; }
        public EventModel Event { get; set; }
        [Key, Column(Order = 2)]
        public string Role { get; set; }
    }
}