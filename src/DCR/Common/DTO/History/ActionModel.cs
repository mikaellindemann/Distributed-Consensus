namespace Common.DTO.History
{
    public class ActionModel
    {
        public int Id { get; set; }
        public string EventId { get; set; }
        public string WorkflowId { get; set; }
        public string CounterpartId { get; set; }
        public int CounterpartTimeStamp { get; set; }
        public ActionType Type { get; set; }
    }

    public enum ActionType
    {
        Includes,
        IncludedBy,
        Excludes,
        ExcludedBy,
        SetsPending,
        SetPendingBy,
        CheckedConditon,
        ChecksConditon,
        ExecuteStart,
        ExecuteFinished
    }
}
