using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models.UriClasses
{
    public class UriRepresentationBase
    {
        [Key, Column(Order = 0)]
        public string WorkflowId { get; set; }
        [Key, Column(Order = 1)]
        public string EventId { get; set; }

        [Key, Column(Order = 2)]
        public string ForeignEventId { get; set; }

        public virtual ServerEventModel Event { get; set; }
    }
}