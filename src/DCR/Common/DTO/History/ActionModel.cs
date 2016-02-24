using System;
using System.Security.AccessControl;

namespace Common.DTO.History
{
    public class ActionModel
    {
        public int Id { get; set; }
        public string EventId { get; set; }
        public string WorkflowId { get; set; }
        public string CounterPartId { get; set; }
        public ActionType Type { get; set; }

        public ActionModel()
        {
            
        }
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
        Locks,
        LockedBy,
        Unlocks,
        UnlockedBy,
        ExecuteStart,
        ExecuteFinished
    }
}
