using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using Common;

namespace Event.Models
{
    public class CheatingType
    {
        [Key, Column(Order = 0)]
        public string WorkflowId { get; set; }
        [Key, Column(Order = 1)]
        public string EventId { get; set; }
        public EventModel Event { get; set; }
        [Key, Column(Order = 2)]
        public CheatingTypeEnum Type { get; set; }
    }
}