namespace Common.DTO.History
{
    public enum ActionType
    {
        Includes,
        IncludedBy,
        Excludes,
        ExcludedBy,
        SetsPending,
        SetPendingBy,
        CheckedConditionBy,
        ChecksCondition,
        CheckedMilestoneBy,
        ChecksMilestone,
        ExecuteStart,
        ExecuteFinished
    }
}
