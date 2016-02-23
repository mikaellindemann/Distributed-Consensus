using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Event.Models.UriClasses
{
    public class UriRepresentationBase
    {
        [Required]
        public string UriString { get; set; }

        [Key, Column(Order = 0)]
        public string WorkflowId { get; set; }
        [Key, Column(Order = 1)]
        public string EventId { get; set; }

        [Key, Column(Order = 2)]
        public string ForeignEventId { get; set; }

        public virtual EventModel Event { get; set; }
    }
}