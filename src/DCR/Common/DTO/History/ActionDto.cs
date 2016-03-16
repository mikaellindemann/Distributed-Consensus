namespace Common.DTO.History
{
    public class ActionDto
    {
        public int TimeStamp { get; set; }
        public string EventId { get; set; }
        public string WorkflowId { get; set; }
        public string CounterpartId { get; set; }
        public int CounterpartTimeStamp { get; set; }
        public ActionType Type { get; set; }

        public ActionDto()
        {
            
        }
        public ActionDto(ActionModel model)
        {
            TimeStamp = model.Id;
            EventId = model.EventId;
            WorkflowId = model.WorkflowId;
            CounterpartId = model.CounterpartId;
            CounterpartTimeStamp = model.CounterpartTimeStamp;
            Type = model.Type;
        }
    }
}
