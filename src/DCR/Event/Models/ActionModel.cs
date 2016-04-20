using System.ComponentModel.DataAnnotations;
using Common.DTO.History;

namespace Event.Models
{
    public class ActionModel
    {
        public int Timestamp { get; set; }
        public string EventId { get; set; }
        public string WorkflowId { get; set; }
        public string CounterpartId { get; set; }
        public int CounterpartTimeStamp { get; set; }
        public ActionType Type { get; set; }


        public ActionDto ToActionDto()
        {
            return new ActionDto
            {
                TimeStamp = Timestamp,
                EventId = EventId,
                WorkflowId = WorkflowId,
                CounterpartId = CounterpartId,
                CounterpartTimeStamp = CounterpartTimeStamp,
                Type = Type
            };
        }
    }
}
