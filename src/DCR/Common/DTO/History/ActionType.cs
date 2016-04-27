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
        CheckedCondition,
        ChecksCondition,
        ExecuteStart,
        ExecuteFinished
    }
}
