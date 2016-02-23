using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Event.Models
{
    /// <summary>
    /// This class is not saved in the database.
    /// </summary>
    public class RelationToOtherEventModel
    {
        public string WorkflowId { get; set; }
        public string EventId { get; set; }
        public Uri Uri { get; set; }
    }
}